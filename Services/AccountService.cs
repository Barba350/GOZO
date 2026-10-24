using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Text.RegularExpressions;
namespace GOZO;
public class AccountService(IAccountRepository repository, IPasswordHasher<Account> hasher)
{
    private const string InvalidLogin = "Usuario o contraseña incorrectos, o acceso temporalmente bloqueado.";
    private static readonly Account Dummy = new() { Username = "unused" };
    // Hash aleatorio solo para igualar el trabajo criptográfico al consultar usuarios inexistentes.
    private static readonly string DummyHash = new PasswordHasher<Account>(Microsoft.Extensions.Options.Options.Create(new PasswordHasherOptions { IterationCount = 210000 })).HashPassword(Dummy, Guid.NewGuid().ToString());
    public async Task<Account> Login(string username, string password, string ip) {
        await using var tx = await repository.Transaction();
        var account = await repository.Find(username.Trim().ToUpperInvariant());
        var result = hasher.VerifyHashedPassword(account ?? Dummy, account?.PasswordHash ?? DummyHash, password);
        if (account == null || !account.Active || account.LockedUntil > DateTime.UtcNow) {
            repository.Audit(username, "Acceso rechazado", ip); await repository.Save(); await tx.Commit(); throw new RuleException(InvalidLogin);
        }
        if (result == PasswordVerificationResult.Failed) {
            account.FailedAttempts++;
            if (account.FailedAttempts >= 5) { account.LockedUntil = DateTime.UtcNow.AddMinutes(15); account.FailedAttempts = 0; }
            repository.Audit(account.Username, "Acceso rechazado", ip); await repository.Save(); await tx.Commit(); throw new RuleException(InvalidLogin);
        }
        account.FailedAttempts = 0; account.LockedUntil = null;
        if (result == PasswordVerificationResult.SuccessRehashNeeded) account.PasswordHash = hasher.HashPassword(account, password);
        repository.Audit(account.Username, "Inicio de sesión", ip); await repository.Save(); await tx.Commit(); return account;
    }
    public static ClaimsPrincipal Principal(Account a) => new(new ClaimsIdentity(new[] {
        new Claim(ClaimTypes.NameIdentifier, a.Id.ToString()), new Claim(ClaimTypes.Name, a.Username),
        new Claim(ClaimTypes.Role, a.Role), new Claim("stamp", a.Stamp),
        new Claim("change_password", a.MustChangePassword ? "yes" : "no")
    }, "GozoCookie"));
    public async Task<bool> Valid(ClaimsPrincipal? principal) {
        if (!int.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return false;
        var a = await repository.Find(id);
        return a != null && a.Active && a.Stamp == principal?.FindFirstValue("stamp") && a.Role == principal.FindFirstValue(ClaimTypes.Role);
    }
    public async Task Create(CreateAccountInput input, string actor, string ip, bool bootstrap = false) {
        if (!Regex.IsMatch(input.Username ?? "", @"^[a-zA-Z0-9._-]{3,40}$")) throw new RuleException("Usuario: 3 a 40 letras, números, punto, guion o guion bajo.");
        ValidatePassword(input.Password);
        if (input.Role is not ("Dueno" or "Cajero")) throw new RuleException("Rol inválido.");
        if (await repository.Find(input.Username.ToUpperInvariant()) != null) throw new RuleException("Ese usuario ya existe.");
        var a = new Account { Username = input.Username, Key = input.Username.ToUpperInvariant(), Role = input.Role, MustChangePassword = !bootstrap };
        a.PasswordHash = hasher.HashPassword(a, input.Password); repository.Add(a);
        repository.Audit(actor, "Creación de usuario: " + input.Username, ip); await repository.Save();
    }
    public static void ValidatePassword(string password) {
        if (password.Length is < 14 or > 128 || password.Distinct().Count() < 5) throw new RuleException("Usa una contraseña de 14 a 128 caracteres, con al menos 5 caracteres diferentes. Puedes usar una frase larga.");
    }
    public async Task ChangePassword(int id, PasswordInput input, string ip) {
        var a = await repository.Find(id) ?? throw new RuleException("Cuenta inexistente.");
        if (hasher.VerifyHashedPassword(a, a.PasswordHash, input.CurrentPassword) == PasswordVerificationResult.Failed) throw new RuleException("La contraseña actual es incorrecta.");
        ValidatePassword(input.NewPassword);
        if (input.NewPassword != input.ConfirmPassword || input.NewPassword == input.CurrentPassword) throw new RuleException("Confirma una contraseña nueva, distinta de la actual.");
        a.PasswordHash = hasher.HashPassword(a, input.NewPassword); a.MustChangePassword = false; a.FailedAttempts = 0; a.LockedUntil = null; a.Stamp = Guid.NewGuid().ToString();
        repository.Audit(a.Username, "Cambio de contraseña y cierre de sesiones", ip); await repository.Save();
    }
    public async Task Toggle(int id, int actorId, string actor, string ip) {
        await using var tx = await repository.Transaction();
        var a = await repository.Find(id) ?? throw new RuleException("Cuenta inexistente.");
        if (id == actorId) throw new RuleException("No puedes desactivar tu propia cuenta.");
        if (a.Active && a.Role == "Dueno" && await repository.ActiveOwners() <= 1) throw new RuleException("Debe quedar al menos un dueño activo.");
        a.Active = !a.Active; a.Stamp = Guid.NewGuid().ToString();
        repository.Audit(actor, (a.Active ? "Activación: " : "Desactivación: ") + a.Username, ip); await repository.Save(); await tx.Commit();
    }
    public async Task ResetPassword(int id, string password, int actorId, string actor, string ip) {
        if (id == actorId) throw new RuleException("Usa Cambiar contraseña para tu propia cuenta.");
        ValidatePassword(password);
        var a = await repository.Find(id) ?? throw new RuleException("Cuenta inexistente.");
        a.PasswordHash = hasher.HashPassword(a, password); a.MustChangePassword = true; a.Stamp = Guid.NewGuid().ToString();
        a.FailedAttempts = 0; a.LockedUntil = null;
        repository.Audit(actor, "Restablecimiento de contraseña: " + a.Username, ip); await repository.Save();
    }
    public async Task<AccountsViewModel> List() => new(await repository.List(), await repository.Events());
    public Task<bool> Any() => repository.Any();
    public async Task Audit(string actor, string action, string ip) { repository.Audit(actor, action, ip); await repository.Save(); }
}

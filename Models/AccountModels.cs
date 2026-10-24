using System.ComponentModel.DataAnnotations;
namespace GOZO;
public class Account
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Key { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Cajero";
    public bool Active { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public string Stamp { get; set; } = Guid.NewGuid().ToString();
    public int FailedAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
}
public class SecurityEvent
{
    public long Id { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Actor { get; set; } = "";
    public string Action { get; set; } = "";
    public string Address { get; set; } = "";
}
public class LoginInput
{
    [Required, StringLength(40)] public string Username { get; set; } = "";
    [Required, StringLength(128)] public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}
public class PasswordInput
{
    [Required, StringLength(128)] public string CurrentPassword { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 14)] public string NewPassword { get; set; } = "";
    [Compare(nameof(NewPassword))] public string ConfirmPassword { get; set; } = "";
}
public class CreateAccountInput
{
    [Required, StringLength(40)] public string Username { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 14)] public string Password { get; set; } = "";
    public string Role { get; set; } = "Cajero";
}
public record AccountsViewModel(List<Account> Accounts, List<SecurityEvent> Events);

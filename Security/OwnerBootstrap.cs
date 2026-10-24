namespace GOZO;
public static class OwnerBootstrap
{
    public static async Task EnsureOwner(IServiceProvider services, IConfiguration config, bool interactive) {
        using var scope = services.CreateScope(); var accounts = scope.ServiceProvider.GetRequiredService<AccountService>();
        if (await accounts.Any()) return;
        var name = config["GOZO_ADMIN_USERNAME"];
        var password = config["GOZO_ADMIN_PASSWORD"];
        if (interactive && (string.IsNullOrWhiteSpace(name) || string.IsNullOrEmpty(password))) {
            Console.Write("Usuario del dueño: "); name = Console.ReadLine();
            Console.Write("Contraseña (mínimo 14 caracteres; no se muestra): ");
            password = ReadPassword(); Console.WriteLine();
        }
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrEmpty(password)) throw new InvalidOperationException("Falta crear al dueño. Ejecuta dotnet run -- --crear-dueno, o configura GOZO_ADMIN_USERNAME y GOZO_ADMIN_PASSWORD como secretos del hosting. No hay contraseña predeterminada.");
        await accounts.Create(new() { Username = name, Password = password, Role = "Dueno" }, "Inicialización", "local", true);
        Console.WriteLine("Cuenta del dueño creada. Retira GOZO_ADMIN_PASSWORD del entorno del hosting.");
    }
    private static string ReadPassword() {
        var password = new System.Text.StringBuilder();
        while (true) {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace) { if (password.Length > 0) password.Length--; }
            else if (!char.IsControl(key.KeyChar) && password.Length < 128) password.Append(key.KeyChar);
        }
        return password.ToString();
    }
}

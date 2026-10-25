# Seguridad de GOZO

Si detectas un fallo, comunícalo en privado al propietario del repositorio. No publiques contraseñas, bases de datos ni información personal en los issues.

Se incluyen autenticación con cookie protegida, roles, hash PBKDF2 mediante PasswordHasher de ASP.NET, cambio de contraseña, revocación de sesiones, CSRF, límites de solicitudes, bloqueo temporal de cuentas, cabeceras y auditoría básica. Estas medidas no constituyen una auditoría independiente ni una garantía contra todos los ataques.

La sesión vence a los 30 minutos y requiere entrar de nuevo. Las cuentas nuevas deben cambiar su clave. Cinco claves incorrectas bloquean la cuenta 15 minutos. Se aplica un límite adicional por dirección de conexión; detrás del proxy puede compartirse entre usuarios. No se confía en X-Forwarded-For recibido de internet. No hay registro público ni credenciales por defecto. Las cuentas desactivadas dejan de funcionar aunque conserven una cookie.

Mantener una sola instancia con SQLite, almacenamiento persistente cifrado por el proveedor y backups propios. Las claves de Data Protection son sensibles: permiten proteger las cookies y deben conservarse fuera de GitHub. El proceso corre con un usuario no root. El puerto interno HTTP solo debe ser accesible por el proxy HTTPS administrado del hosting. GOZO_TLS_TERMINATION=Edge no instala TLS: requiere el proxy real que termina TLS y redirige HTTP a HTTPS. No expongas el puerto 8080 directamente a internet.

CSP bloquea scripts externos, marcos, objetos y formularios externos; se permiten estilos inline por el diseño existente. No se permiten CORS cruzados. El sitio no almacena ventas en localStorage ni incluye service worker que guarde datos privados para uso sin conexión.

Las advertencias de dependencias de GitHub Actions deben revisarse antes de desplegar. No hay WAF ni protección de DDoS volumétrico en el código: dependen del hosting. No se incluye MFA en esta entrega. Activa MFA en GitHub y en el hosting. Los logs de auditoría no son inmutables y deben respaldarse. No se guarda propietario/cajero en la venta histórica; la auditoría registra el actor y la ruta de las escrituras sin copiar datos de clientes.

La aplicación conserva .NET 8 para mantener compatibilidad con el proyecto de Visual Studio. Revisa su ciclo de soporte oficial y migra a una versión LTS soportada antes de que termine el soporte. No publiques con un runtime sin soporte; el Dockerfile usa la etiqueta 8.0 con sus actualizaciones disponibles, pero requiere reconstrucción para aplicarlas.

Referencias:
- https://learn.microsoft.com/aspnet/core/security/authentication/cookie?view=aspnetcore-8.0
- https://learn.microsoft.com/aspnet/core/security/anti-request-forgery?view=aspnetcore-8.0
- https://learn.microsoft.com/aspnet/core/performance/rate-limit?view=aspnetcore-8.0
- https://render.com/docs/disks


## PostgreSQL para internet (octubre 2026)

SQLite sigue disponible localmente. Se añadió PostgreSQL para ventas, cuentas y claves de sesión persistentes en el alojamiento gratuito. Ver PUBLICAR.md. No se importan automáticamente datos locales. La publicación requiere conectar Neon y Render; no se han creado recursos de pago. La integración PostgreSQL se verifica con PostgreSQL 16 desechable en CI.


## Menú y reportes para cajeros

Se añaden siete categorías del menú: Sándwiches y croissants, Salchipapas, Platos de pollo, Empanadas, Cafetería, Jugos naturales y Postres. Se conservan Helados, Hamburguesas y Bebidas con sus IDs; el arranque agrega solo categorías faltantes y no carga ni cambia productos o precios. Dueños y cajeros pueden consultar Reportes y descargar PDF/Excel de todas las ventas del periodo. Usuarios, catálogo administrativo, promociones e historial individual de clientes siguen restringidos al dueño. Los nombres de usuario admiten espacios y letras con acentos; por ejemplo, Luis Heredia.

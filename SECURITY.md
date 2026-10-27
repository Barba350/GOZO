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


## Pedidos por WhatsApp

Dueños y cajeros registran manualmente pedidos con receptor, celular de 8 dígitos, productos, notas y cliente registrado opcional (con CI). No se conecta a WhatsApp ni se importan chats. Estados: Pendiente, Entregado y Cancelado. Al entregar se exige Efectivo o QR; se crea una única venta con fecha de entrega y los importes/descuentos acordados al registrar el pedido. Los pendientes y cancelados no suman ingresos. No se permite cancelar pedidos ya entregados ni entregar cancelados. Reintentos y operaciones simultáneas se protegen con transacciones, revisión de concurrencia e índices únicos. No se editan ni eliminan pedidos en esta versión.

La migración aditiva 001-orders está versionada en Data/Migrations para SQLite y PostgreSQL; se ejecuta al iniciar, sobre instalaciones nuevas y existentes, y registra su versión. No modifica tablas ni ventas previas. Pruebas HTTP incluyen conversión a venta, cancelación, reintentos, concurrencia, permisos y persistencia tras reinicio. La lista muestra hasta 200 pedidos recientes por estado.


## Promociones sobre el total y límite por cliente

Las promociones se aplican una sola vez al subtotal completo de la venta, con uno o varios productos; se usa la mejor promoción elegible sin acumular descuentos. MONTO es un monto por venta; PORCENTAJE se calcula sobre el total. Se requiere seleccionar un cliente registrado con CI. El límite configurable es por promoción y por cliente, de 1 a 999 compras (por defecto 1); no es un límite de unidades ni un límite diario. El monto se reparte en centavos entre líneas únicamente para que ticket, PDF, Excel y totales coincidan.

Las ventas guardadas consumen un uso. Los pedidos pendientes con promoción reservan un uso, cancelar libera la reserva y entregar conserva un solo uso sin duplicarlo. Se serializa el cálculo y guardado por cliente para evitar que dos cajas superen la cuota. Las cotizaciones no consumen usos; al guardar se recalcula y se rechaza una confirmación con total desactualizado.

La migración aditiva 002 conserva precios y descuentos históricos e incorpora los usos antiguos identificables por el nombre de promoción almacenado en el detalle. Si una promoción antigua cambió de nombre, esa asociación histórica puede no reconstruirse automáticamente. No se identifican compras anónimas por nombre o celular: se exige CI para nuevas promociones. Las asociaciones antiguas a productos se conservan como historial hasta editar la promoción, pero dejan de restringir los nuevos descuentos globales.


## Ajuste vigente: descuento por unidad y selección de todos los clientes

El descuento vuelve a calcularse por unidad de los productos elegidos; MONTO se descuenta por cada unidad y PORCENTAJE se redondea al centavo por unidad. Se elige la mejor promoción elegible para cada producto, sin acumular dos promociones en la misma unidad. Los productos no seleccionados mantienen su precio (vacío = todos). El límite por cliente y promoción se mantiene: una compra con varias unidades consume un solo uso de cada promoción aplicada. Los usos anteriores y las reservas de pedidos continúan vigentes. No se recalculan ventas ni pedidos ya guardados.

El formulario permite Seleccionar todos los clientes mediante una casilla, con casillas individuales y estado parcial. Todos incluye también clientes registrados posteriormente; al desmarcar algunos se guarda una lista específica. Si se desmarcan todos, el formulario exige elegir al menos uno o volver a marcar Todos.

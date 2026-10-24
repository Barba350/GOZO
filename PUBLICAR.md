# GOZO por internet sin contratar hosting de pago

La aplicación sigue siendo C# ASP.NET Core MVC, Services y Repository.
SQLite se conserva para uso local. La versión de internet usa PostgreSQL externo
para conservar ventas, clientes, cuentas y claves de sesión aunque Render reinicie.
Las dos instalaciones son independientes: no sincronizan ni importan automáticamente datos.

## Servicios

1. Conectar GitHub, Neon y Render. Elegir exclusivamente planes Free.
2. En Neon crear un proyecto y base vacíos para GOZO. No activar planes de pago.
3. En Render crear el servicio desde este repositorio con `render.yaml`, Docker y plan Free.
   No añadir disco ni base de datos de Render. La configuración no crea recursos de pago.
4. Configurar los secretos en Render, nunca en GitHub ni en mensajes:

| Variable | Valor |
| --- | --- |
| GOZO_DB_PROVIDER | PostgreSQL |
| GOZO_POSTGRES_CONNECTION | Host=HOST_NEON;Database=neondb;Username=USUARIO;Password=CONTRASEÑA;SSL Mode=VerifyFull |
| GOZO_PUBLIC_ORIGIN | URL HTTPS exacta del servicio, sin rutas |
| GOZO_TLS_TERMINATION | Edge |
| GOZO_ADMIN_USERNAME | Nombre del dueño |
| GOZO_ADMIN_PASSWORD | Clave única de al menos 14 caracteres |

Usar los campos de conexión de Neon en formato Npgsql, no pegar una URL
`postgresql://` en la variable. `VerifyFull` verifica TLS y el nombre del servidor.
El permiso de prueba sin TLS solo se acepta en Development; jamás activarlo en Render.
El puerto interno es 8080. `/salud` confirma arranque; comprobar además acceso,
registro de una venta de prueba, reinicio y conservación de datos y sesión.

5. Tras crear el dueño, quitar GOZO_ADMIN_PASSWORD del entorno y volver a desplegar.
   La cuenta existente permanece. Crear cajeros desde Usuarios y usar claves únicas.
6. Abrir la URL HTTPS desde PC o móvil. Guardarla en la pantalla de inicio si se desea.

## Límites y datos existentes

Render Free puede suspenderse por inactividad: la primera carga puede tardar.
Neon Free tiene cuotas de almacenamiento y cómputo. No contratar ampliaciones
sin autorización; revisar los límites vigentes en los paneles oficiales.
No se promete disponibilidad continua ni sustitución de copias de seguridad.

La inicialización crea tablas en una base vacía, con cuentas y claves en el esquema
`gozo_accounts`. No altera ni importa las bases SQLite existentes. Futuras modificaciones
de esquema necesitan migraciones versionadas antes de desplegarse.
Las claves de protección persisten en PostgreSQL; proteger el acceso y backups de esa
base, porque también contiene credenciales cifradas mediante hash y claves de sesión.
Para llevar ventas locales a internet, hacer primero una copia y una importación
validada explícita. No subir archivos `.db`, claves ni secretos al repositorio.

Documentación: https://render.com/docs/free y https://neon.com/pricing.

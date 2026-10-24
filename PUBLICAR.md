# Publicación gratuita de GOZO — estado actual

Presupuesto solicitado: cero pago de hosting. No se ha creado ningún servicio de pago ni se debe usar la configuración de disco `starter` de la revisión anterior. El Blueprint de pago fue retirado de este repositorio.

## Lo que ya está preparado

- Código ASP.NET Core MVC, Services y Repository.
- Inicio de sesión y roles dueño/cajero.
- Protección CSRF, contraseñas con hash, bloqueo temporal, auditoría y sesiones revocables.
- Interfaz móvil y ticket adaptable.
- Dockerfile y GitHub Actions para compilación y pruebas.
- Exclusión de bases, clientes y secretos de GitHub.

## Decisión pendiente

El proyecto actual usa SQLite local. Render Free no conserva las escrituras en su sistema de archivos al reiniciar ni permite disco persistente. Publicar allí esta versión tal como está podría perder ventas, cuentas y claves de sesión; por ello el inicio de producción se bloquea si no se declara un disco SQLite persistente real mediante `GOZO_PERSISTENT_SQLITE=true`. No actives ese indicador en un servicio gratuito sin almacenamiento permanente.

La propuesta sin pago es usar Render Free para la aplicación y Neon Free (PostgreSQL) para los datos, manteniendo SQLite para el desarrollo local. Ese cambio de proveedor todavía requiere confirmación del usuario y adaptación del código. No basta con cambiar una cadena de conexión: hay que adaptar inicialización, cuentas y persistencia de las claves que protegen las cookies, además de probar el flujo completo. No hay una versión PostgreSQL terminada en este commit.

## Próximos pasos después de confirmar PostgreSQL

1. Habilitar Render y Neon en la conversación.
2. Mantener los planes Free. No activar mejoras de pago.
3. Adaptar y probar el proyecto con ambos proveedores.
4. Publicar solo después de que GitHub Actions pase.
5. Configurar la contraseña inicial del dueño como secreto del hosting y retirarla tras crear la cuenta.
6. Verificar el enlace HTTPS desde computadora y celular.

Render Free se suspende tras 15 minutos sin visitas y puede tardar alrededor de un minuto en despertar. Neon Free también tiene límites de almacenamiento y cómputo. Esta opción sirve para demostración y uso ligero; no ofrece la disponibilidad de un servicio de producción de pago.

Referencias verificadas:
- https://render.com/docs/free
- https://neon.com/docs/introduction/plans

La versión local continúa funcionando: `dotnet run -- --crear-dueno` y luego `dotnet run`. Conserva tu carpeta App_Data local. Nunca envíes contraseñas ni bases de clientes al repositorio.

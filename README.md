# GOZO — MVC, móvil y acceso protegido

C# ASP.NET Core MVC 8, Services, Repository, vistas Razor, Entity Framework y SQLite. Basado en los diez HU y 43 criterios de los Excel originales, con CI único, celular de 8 dígitos, logo PNG y ticket solicitado. Esta revisión añade cuentas, permisos y preparación para despliegue HTTPS. No está publicado todavía: ver PUBLICAR.md.

## Ejecutar en tu computadora

1. Descomprime el ZIP y abre GOZO.sln en Visual Studio 2022 con el SDK .NET 8 y la carga ASP.NET y web.
2. Antes del primer inicio, abre la terminal en esta carpeta y ejecuta:

```sh
dotnet run -- --crear-dueno
```

3. Introduce tu usuario y una contraseña de al menos 14 caracteres. La contraseña no se muestra. No hay una contraseña incluida en el ZIP. El comando crea al dueño y termina; no cambia cuentas existentes.
4. Ejecuta con Ctrl+F5 o `dotnet run` y entra a http://localhost:5080. El perfil GOZO configura Development para la prueba local. También puedes usar INICIAR.bat.
5. Inicia sesión. Desde **Usuarios y seguridad**, el dueño puede crear cuentas, desactivarlas y restablecer claves temporales. Los usuarios nuevos deben cambiar su clave antes de acceder a los módulos.

Si una instalación anterior ya tiene ventas, conserva tu `App_Data/gozo.db`. La seguridad se guarda en una base separada `accounts.db`. No borres App_Data para solucionar errores: allí están los datos.

## Funciones y permisos

| Función | Cajero | Dueño |
| --- | --- | --- |
| Registrar ventas con efectivo/QR, descuentos automáticos y cliente opcional | Sí | Sí |
| Registrar y buscar clientes por CI, nombre o celular | Sí | Sí |
| Crear/editar/retirar productos | No | Sí |
| Administrar promociones | No | Sí |
| Ver reportes, exportar PDF/Excel e historial por cliente | No | Sí |
| Crear/desactivar cuentas y restablecer contraseñas | No | Sí |
| Cambiar su propia contraseña y cerrar sesión | Sí | Sí |

El servidor aplica los permisos incluso si se manipula el menú o se llama directamente a la API. La sesión vence a los 30 minutos. Cada cuenta es individual.

## Reglas del negocio

- Categorías iniciales: Helados, Hamburguesas y Bebidas. El catálogo inicial está vacío.
- CI obligatorio y único. Se permiten nombres repetidos y un celular compartido. Se normalizan espacios/puntos del CI y se conserva el complemento con guion si corresponde.
- Registro de cliente: nombre, CI y celular nacional de 8 dígitos. No se solicita correo.
- Producto duplicado: mismo nombre normalizado y categoría, incluso si está retirado. Precio positivo, hasta dos decimales.
- Baja lógica: las ventas conservan nombre y precio históricos. Dinero almacenado en centavos enteros.
- Venta con detalle guardada en transacción; el servidor calcula precios y descuentos, exige efectivo o QR y evita duplicar reintentos con identificador de operación. Si cambió el total después de revisar, exige recalcular antes de cobrar.
- QR registra el método de cobro; no genera un QR bancario ni procesa transferencias.
- Promociones por porcentaje o monto por unidad, con fechas y clientes opcionales. Se usa la mejor promoción por producto, sin acumular; total mínimo cero. El porcentaje se redondea al centavo por línea.
- Fechas de negocio en Bolivia (UTC−4). Filtros por hoy, semana desde lunes, mes o rango personalizado, incluyendo todo el último día.
- Reportes netos de descuentos. Efectivo + QR coincide con ingresos. Exportaciones corresponden al último reporte generado.
- Ticket: búsqueda de cliente con CI visible, cantidades, descuentos, total, Efectivo/QR, Cobrar y Vaciar ticket.

## Arquitectura

**Vista → Controller → Service → Repository → SQLite.** La inyección de dependencias usa una instancia por solicitud. Los Controllers y Services del negocio no consultan el DbContext directamente. El código de bootstrap configura las bases al iniciar. Los repositorios coordinan las escrituras con UnitOfWork.

| Carpeta/archivo | Contenido |
| --- | --- |
| Models | Entidades, DTO y ViewModels |
| Views / Controllers | MVC Razor y acciones JSON bajo /api |
| Services | Reglas de negocio y autenticación |
| Repositories | Persistencia y transacciones |
| Data | Contextos de negocio y cuentas |
| Security / Middleware | Sesiones, CSRF, límites y cabeceras |
| wwwroot | CSS, JS, logo PNG y manifiesto móvil |
| Dockerfile / PUBLICAR.md | Empaquetado y estado de la publicación gratuita |
| .github | CI y Dependabot |
| tests | Pruebas de ticket, integración, exclusión de datos y dependencias |

Para ventas y gestión se utilizan vistas Razor reales; JavaScript aporta interacción con fetch. Las operaciones que cambian datos requieren un token CSRF. La pantalla de acceso usa formularios Razor, sin scripts externos.

## Publicar y usar desde móvil

Sigue **PUBLICAR.md**. La configuración de hosting de pago fue retirada. La propuesta gratuita con PostgreSQL está pendiente de confirmación y adaptación; no publiques SQLite en almacenamiento temporal. GitHub guarda el código; el hosting ejecuta ASP.NET. SQLite debe vivir en un disco persistente del servidor. No hay una app Android separada: se accede por HTTPS desde cualquier lugar. El menú y ticket se adaptan al teléfono. Puedes añadir el enlace a la pantalla de inicio; requiere conexión.

El ZIP no contiene App_Data, usuarios, contraseñas, claves ni datos de clientes. La app crea sus bases en el primer inicio. `schema.sql` es una referencia del esquema de negocio, no hay que ejecutarlo sobre una base existente.

## Seguridad y límites

Ver **SECURITY.md**. Contraseñas con hash mediante PasswordHasher, cookies HttpOnly/SameSite y Secure en producción, CSRF en las escrituras, roles, bloqueo temporal y rate limiting, auditoría básica, cabeceras y protección frente a inclusión en marcos. Se conserva la corrección del fallo Windows EventLog: los mensajes se escriben en consola y Debug.

No hay registro público, cuentas demo ni secretos predeterminados. No hay autenticación multifactor dentro de GOZO, pentest independiente ni garantía de protección absoluta. El hosting debe aportar TLS, aislamiento, almacenamiento persistente y medidas de disponibilidad. SQLite está preparado para una instancia del servidor; no escales a varias réplicas con la misma base de archivo.

## Verificación

El entorno local de edición no dispone de .NET ni Docker. La verificación se ejecutó en GitHub Actions: compilación C#/Razor, scripts, ticket, integración HTTP del negocio y seguridad, auditoría de dependencias y exclusión de bases/secretos terminaron correctamente. Evidencia de la revisión de código: https://github.com/Barba350/GOZO/actions/runs/40059932938 (commit 54f1126). La imagen Docker, la revisión visual en móviles y un despliegue público real todavía no están verificados. No hay una auditoría independiente ni una versión PostgreSQL completada.

```sh
dotnet build
node tests/ticket-ui.test.cjs
python tests/aceptacion.py
```

La integración crea una copia temporal y bases propias: no modifica tus ventas. Comprueba negocio y acceso anónimo, CSRF, roles, cambio obligatorio de clave, hash, bloqueo, revocación y cabeceras. Los secretos de prueba se generan al ejecutar y no se almacenan en el código. Las pruebas del navegador móvil y el flujo completo de producción siguen pendientes de ejecución real.

Para defenderlo: «MVC separa la presentación, los datos y los controladores; Services contiene las reglas y Repository accede a SQLite. Las cuentas separan funciones de dueño y cajero. El servidor central permite trabajar desde computadora y celular con la misma información».

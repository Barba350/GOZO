# Matriz de criterios de aceptación — GOZO

Fuente: `Historias_Usuario_SCRUM_GOZO_2.xlsx`, hoja principal. Se incluyen los **43 criterios** de las diez historias; la columna de prueba indica cómo revisarlos, no que ya hayan pasado una prueba integral.

**Estado:** implementados en código. GitHub Actions compiló C#/Razor y ejecutó las pruebas HTTP de negocio y seguridad correctamente (ejecución 40059932938). La columna de verificación sigue siendo una propuesta de revisión manual de interfaz; no afirma que se hayan probado visualmente los 43 CA. Ver `README.md` y `tests/aceptacion.py`.

| Historia / CA | Criterio del Excel | Pantalla | Verificación propuesta |
| --- | --- | --- | --- |
| HU-01 · CA 1 | El cajero debe poder seleccionar uno o varios productos del catálogo. | Nueva venta | Agregar dos productos al carrito. |
| HU-01 · CA 2 | El sistema debe calcular automáticamente el total de la venta. | Nueva venta | Modificar cantidades y comprobar la suma. |
| HU-01 · CA 3 | La venta debe poder registrarse con la forma de pago seleccionada (efectivo o QR). | Nueva venta | Confirmar ventas con efectivo y QR. |
| HU-01 · CA 4 | La venta puede asociarse opcionalmente a un cliente ya registrado en HU-02, seleccionándolo; no se crea ni edita el cliente desde la venta. | Nueva venta | Guardar sin cliente y luego con cliente; comprobar que no hay formulario de alta dentro de la venta. |
| HU-02 · CA 1 | El sistema debe permitir registrar nombre y datos de contacto del cliente. | Clientes | Registrar nombre, CI y contacto. |
| HU-02 · CA 2 | El cajero debe poder buscar un cliente ya registrado. | Clientes | Buscar por nombre, CI o contacto. |
| HU-02 · CA 3 | El sistema debe evitar el registro de clientes duplicados. | Clientes | Repetir CI con puntos/espacios: debe rechazarse; registrar igual nombre con otro CI: debe permitirse. |
| HU-03 · CA 1 | El sistema debe permitir seleccionar entre efectivo o QR. | Nueva venta / Reportes | Elegir las dos opciones del selector de pago. |
| HU-03 · CA 2 | La venta debe quedar asociada a la forma de pago seleccionada. | Nueva venta / Reportes | Ver el método en la venta del reporte. |
| HU-03 · CA 3 | El sistema debe permitir generar un reporte de ingresos por forma de pago. | Nueva venta / Reportes | Comprobar Efectivo + QR = ingresos. |
| HU-03 · CA 4 | El sistema debe validar que se haya seleccionado una forma de pago antes de cerrar la venta. | Nueva venta / Reportes | Intentar confirmar sin pago: debe mostrar error y no guardar. |
| HU-04 · CA 1 | El dueño debe poder agregar un nuevo producto con nombre, precio y categoría. | Productos | Crear producto con los tres campos. |
| HU-04 · CA 2 | Los productos deben clasificarse en categorías (Helados, Hamburguesas, Bebidas). | Productos | Comprobar las tres categorías iniciales. |
| HU-04 · CA 3 | El sistema debe evitar productos duplicados. | Productos | Repetir nombre/categoría variando mayúsculas y espacios: debe rechazarse. |
| HU-05 · CA 1 | El dueño debe poder editar el precio o los datos (nombre, categoría) de un producto existente. | Productos / Nueva venta | Editar nombre, precio y categoría desde el catálogo. |
| HU-05 · CA 2 | El sistema debe evitar que la edición deje un producto duplicado. | Productos / Nueva venta | Editar para coincidir con otro producto: debe rechazarse. |
| HU-05 · CA 3 | El cambio debe reflejarse en las nuevas ventas. | Productos / Nueva venta | Vender después de editar y comprobar precio nuevo e historial anterior intacto. |
| HU-06 · CA 1 | El dueño debe poder eliminar un producto del catálogo. | Productos / Reportes | Retirar producto y confirmar la acción. |
| HU-06 · CA 2 | El producto eliminado no debe aparecer en el catálogo al registrar una venta. | Productos / Reportes | Volver a Venta y comprobar que no se ofrece. |
| HU-06 · CA 3 | Las ventas ya registradas deben conservar el producto en su detalle. | Productos / Reportes | Consultar ventas previas y comprobar que el detalle no cambia. |
| HU-07 · CA 1 | El dueño debe poder crear una promoción con un nombre y descripción. | Promociones / Nueva venta | Crear promoción con nombre y descripción obligatorios. |
| HU-07 · CA 2 | Debe poder asociarse a uno o varios productos. | Promociones / Nueva venta | Seleccionar varios productos con Ctrl+clic. |
| HU-07 · CA 3 | Debe poder definirse un porcentaje o monto de descuento. | Promociones / Nueva venta | Probar 10% y Bs 3 por unidad. |
| HU-07 · CA 4 | Debe poder definirse una fecha de inicio y fin para la promoción. | Promociones / Nueva venta | Probar fechas válidas y rango invertido; verificar fuera de vigencia. |
| HU-07 · CA 5 | El sistema debe aplicar automáticamente el descuento al momento de la venta. | Promociones / Nueva venta | Vender con promoción y ver descuento en carrito y venta guardada. |
| HU-07 · CA 6 | El sistema debe mostrar las promociones activas. | Promociones / Nueva venta | Ver estado Activa/Programada/Vencida/Desactivada en el listado. |
| HU-07 · CA 7 | El dueño debe poder desactivar una promoción antes de su fecha de fin. | Promociones / Nueva venta | Desactivar y comprobar que una venta nueva no recibe el descuento. |
| HU-08 · CA 1 | El sistema debe permitir seleccionar un rango de fechas (día, semana, mes). | Reportes | Elegir hoy, esta semana, este mes y rango personalizado. |
| HU-08 · CA 2 | El reporte debe mostrar el total de ingresos del período seleccionado. | Reportes | Sumar ventas conocidas y comparar con ingresos. |
| HU-08 · CA 3 | El reporte debe mostrar el producto más vendido. | Reportes | Comparar producto más vendido con unidades de los detalles. |
| HU-08 · CA 4 | El reporte debe mostrar la cantidad de ventas realizadas. | Reportes | Comparar cantidad de ventas con el historial. |
| HU-08 · CA 5 | El reporte debe poder generarse con un clic. | Reportes | Cambiar rango y pulsar Generar reporte una vez. |
| HU-08 · CA 6 | El sistema debe mostrar el historial de ventas del período. | Reportes | Ver tabla del periodo y desplegar sus detalles. |
| HU-08 · CA 7 | El reporte debe poder visualizarse en pantalla. | Reportes | Consultar tarjetas, pagos, ranking e historial en pantalla. |
| HU-08 · CA 8 | El reporte debe poder exportarse (PDF o Excel). | Reportes | Descargar PDF y XLSX y abrir ambos; contrastar totales con pantalla. |
| HU-09 · CA 1 | El sistema debe calcular la cantidad vendida por producto. | Reportes | Sumar cantidades por producto en ventas conocidas. |
| HU-09 · CA 2 | El sistema debe ordenar los productos de mayor a menor cantidad vendida. | Reportes | Comprobar orden de mayor a menor; empates por nombre. |
| HU-09 · CA 3 | Debe poder filtrarse por período (día, semana, mes). | Reportes | Cambiar día/semana/mes y generar de nuevo. |
| HU-09 · CA 4 | El resultado debe mostrarse de forma clara (listado o gráfico). | Reportes | Ver ranking con barras y cantidades explícitas. |
| HU-10 · CA 1 | El sistema debe mostrar todas las compras asociadas a un cliente. | Historial de cliente | Seleccionar cliente por CI y consultar todas sus compras. |
| HU-10 · CA 2 | El historial debe incluir fecha, productos y monto de cada compra. | Historial de cliente | Desplegar fecha, productos y montos de cada venta. |
| HU-10 · CA 3 | El sistema debe permitir filtrar el historial por fechas. | Historial de cliente | Acotar fechas y comprobar exclusión de ventas fuera del rango. |
| HU-10 · CA 4 | El sistema debe calcular el total gastado por el cliente. | Historial de cliente | Sumar ventas y comparar con Total gastado por el cliente. |

## Ampliación solicitada: CI del cliente

- Nombre y contacto siguen disponibles.
- CI obligatorio, normalizado y único en SQLite.
- Dos clientes pueden compartir nombre y contacto si tienen CI distintos.
- Búsqueda y selección muestran CI para distinguir homónimos.
- CI aparece en historial, PDF y Excel de ventas.
- Se reemplaza la antigua regla nombre+contacto como clave de duplicados por CI, de acuerdo con la ampliación solicitada.

## Subtareas del plan de sprints

Las subtareas funcionales del plan se concentran en `Models/` y `Data/GozoDb.cs` (tablas/índices), `Controllers/` (HTTP y vistas), `Services/` (reglas y consultas), `Repositories/` (acceso a datos), `Views/` y `wwwroot/` (pantallas), `Exports.cs` (PDF/XLSX) y `tests/aceptacion.py` (pruebas integrales propuestas). Los endpoints se sirven bajo el prefijo `/api`, por ejemplo `POST /api/productos`, `POST /api/ventas` y `GET /api/clientes?buscar=`. Las actividades de QA de Jira no se consideran cerradas hasta ejecutar las pruebas en .NET y comprobar la interfaz.

## Última ampliación: celular

El campo de contacto solicitado por los criterios originales se concreta como número de celular obligatorio de 8 dígitos. No se solicita correo. El CI sigue siendo el identificador único.

## Ampliación: acceso por internet y permisos

La revisión agrega inicio de sesión obligatorio, cuentas de dueño/cajero, CSRF, límites de solicitudes, bloqueo temporal y auditoría básica. Catálogo administrativo, promociones e historial de clientes requieren dueño; ventas y registro/búsqueda de clientes admiten cajeros. Las pruebas de integración ahora inician sesión y usan tokens CSRF. La suite .NET ya fue ejecutada; la verificación visual móvil sigue pendiente. Ver SECURITY.md y PUBLICAR.md.

## Verificación en GitHub

Compilación y suite HTTP de negocio/seguridad: https://github.com/Barba350/GOZO/actions/runs/40059932938. Las pruebas visuales de móvil siguen pendientes; la adaptación PostgreSQL se añadió después con una suite separada.


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

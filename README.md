# GOZO · Ventas y control de caja

ASP.NET Core MVC 8, Services, Repository y Entity Framework. La instalación de internet en Render usa PostgreSQL en Neon (`GOZO_DB_PROVIDER=PostgreSQL`). La conexión externa sigue en secretos del alojamiento. SQL Server puede usarse en otra instalación con `GOZO_DB_PROVIDER=SQLServer` y `GOZO_SQLSERVER_CONNECTION`. No hay proveedor SQLite en esta revisión.

## Cambios de operación

- Dos sucursales con catálogo, clientes, promociones y cupones compartidos.
- Caja individual por usuario y sucursal. Una persona no puede mantener dos cajas abiertas, tampoco cambiando de sucursal. Dos cajeros distintos sí pueden tener cada uno su caja en la misma sucursal.
- Abrir exige solo el fondo inicial recibido. Conteo por denominaciones opcional y plegable; si se adjunta debe coincidir con el importe.
- Toda venta o entrega de pedido exige la caja abierta del usuario que cobra, incluso QR o canje de cero.
- Cierre: efectivo contado y QR confirmado manualmente, con diferencias independientes. Cualquier diferencia exige observaciones de al menos diez caracteres. Cierra su cajero o el dueño.
- Cajeros ven solo sus cajas. Panel del equipo: últimos 30 días de la sucursal, productos por unidades entregadas (incluye regalos) y clientes registrados por número de compras; no muestra ingresos, CI ni celulares.
- Solo el dueño consulta Reportes, PDF y Excel. Puede filtrar fechas, sucursal y cajero y ver resumen por cajero/sucursal, detalle y aperturas/arqueos del periodo. PDF y Excel incluyen quién cobró, sucursal y número de caja por venta.
- Las ventas nuevas guardan el identificador y nombre de quien confirma la operación, la sucursal y la caja. Entregar un pedido atribuye la venta al usuario que lo entrega/cobra.
- Registros anteriores se conservan como históricos, con cajero/sucursal/caja sin registro cuando no se conocían. No se inventan atribuciones.
- Cupones de monto, porcentaje o producto específico con unidades gratuitas. Un solo canje global, reserva en pedidos pendientes y liberación al cancelar; no se acumulan con promociones. Total cero se registra CANJE sin ingreso.
- Correcciones de cantidad/pago solo por el dueño mientras la caja original está abierta, conservando condiciones originales, con justificación y auditoría de antes/después.

La aplicación no consulta bancos ni concilia QR automáticamente, no incluye retiros/gastos de caja, inventario, facturación fiscal ni reembolsos bancarios. Cambiar el nombre de un usuario no cambia la propiedad de sus cajas: se filtran por su identificador.

## Datos y seguridad

Una base externa para negocio y cuentas (`gozo_accounts`), fotos en la base y claves de protección persistentes. Sesión con verificación de sello, roles, CSRF, límite de intentos y encabezados de seguridad. Las comprobaciones de propiedad y permisos se hacen en servidor; ocultar un menú no es la protección.

Migración aditiva `004_operations.postgresql.sql`, idempotente, ejecutada bajo bloqueo de inicialización y transacción. Los históricos no se recalculan. Transacciones y bloqueos serializan cobros/cierres por sucursal, cuota por cliente y canjes por código. No se distribuyen contraseñas de usuarios ni conexiones reales.

Producción: `GOZO_PUBLIC_ORIGIN=https://dominio`, `GOZO_TLS_TERMINATION=Edge` solo detrás de proxy HTTPS administrado. La instalación existente conserva usuarios y contraseñas; el bootstrap por variables solo crea al primer dueño de una base vacía.

## Verificación

`dotnet build -c Release`, pruebas Node del ticket/formularios y pruebas HTTP contra PostgreSQL desechable en GitHub Actions. `tests/aceptacion.py` cubre catálogo, clientes, promociones/cuotas, fotos, pedidos, persistencia, seguridad y exportaciones. `tests/cashier_integration.py` cubre propiedad de cajas, efectivo/QR, filtros/reportes de dueño, dashboard, correcciones y canjes. Nunca deben ejecutarse sobre la base real.

La migración se ensaya primero en una rama temporal de Neon. La producción se verifica después del despliegue sin crear ventas de prueba.

## Cliente obligatorio en nuevas ventas

El cajero debe elegir un cliente registrado o pulsar **Usar Consumidor final** antes de guardar. No hay selección automática. Consumidor final es un único registro interno sin CI ni celular, creado idempotentemente por la migración 005. No se inventan datos personales.

Las cotizaciones pueden calcular el carrito antes de elegir, pero guardar sin CustomerId o con un cliente inexistente se rechaza en servidor. Los pedidos pendientes sin cliente exigen seleccionarlo al entregar; se conserva su precio acordado. Históricos sin cliente permanecen intactos.

Consumidor final no aparece como un comprador frecuente ni recibe promociones ligadas al CI; sí puede usar cupones generales válidos de un solo uso. No se ofrece como destinatario de cupones personales ni de promociones específicas. Se excluye del listado de clientes identificados y del historial individual, pero sus ventas sí se ven en los reportes del dueño.

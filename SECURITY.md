# Seguridad de GOZO

Reportes, PDF, Excel, promociones, cupones, usuarios y correcciones solo para dueño. Cajeros registran ventas/pedidos/clientes y acceden a su panel y caja. Las cajas se filtran en servidor por el ID del usuario autenticado, no por el nombre ni por un ID enviado por el navegador. El dueño puede supervisar y cerrar cajas de la sucursal seleccionada.

El servidor calcula precios, descuentos, ingresos y diferencias de efectivo/QR. QR confirmado es una declaración manual, no verificación bancaria. Cualquier diferencia exige justificación. Cierres definitivos y cambios de ventas auditados. Nuevas ventas guardan usuario, sucursal y caja; históricos sin atribución se conservan como desconocidos.

Se mantienen cookies seguras, verificación de cuenta/sello por solicitud, CSRF, bloqueo de intentos, password hashing y encabezados CSP. No hay credenciales predeterminadas ni secretos en el repositorio. El dueño existente no se restablece en un despliegue.

La versión de Render usa PostgreSQL con certificado verificado. SQL Server requiere Encrypt=True y TrustServerCertificate=False en producción. Excepciones TLS solo para pruebas explícitas de desarrollo. Configurar proxy HTTPS sin acceso público al puerto interno. Mantener backups y permisos de base adecuados.

No hay pentest independiente, consulta bancaria, control fiscal ni auditoría externa inmutable. Un administrador de base sigue teniendo acceso a los datos.

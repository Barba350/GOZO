#!/bin/sh
set -eu
umask 077
if [ "${GOZO_DATA_DIR:-/data}" != "/data" ]; then
  echo 'En esta imagen GOZO_DATA_DIR debe ser /data, con el disco persistente montado allí.' >&2
  exit 1
fi
mkdir -p /data
# Inicialización del volumen como root; el servidor se ejecuta como app.
chown -R --no-dereference app:app /data
find /data -xdev -type d -exec chmod 700 {} +
find /data -xdev -type f -exec chmod 600 {} +
exec gosu app dotnet GOZO.dll "$@"

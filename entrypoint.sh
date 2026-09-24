#!/bin/sh
set -e

PUID=${PUID:-1654}
PGID=${PGID:-1654}

if [ "$PUID" -eq 0 ] || [ "$PGID" -eq 0 ]; then
    exec dotnet FunkArr.dll
fi

group=$(getent group "$PGID" | cut -d: -f1 || true)
if [ -z "$group" ]; then
    addgroup -g "$PGID" funkarr
    group=funkarr
fi

if ! getent passwd "$PUID" > /dev/null 2>&1; then
    adduser -u "$PUID" -G "$group" -D -h /app -s /sbin/nologin funkarr
fi

user=$(getent passwd "$PUID" | cut -d: -f1)

chown -R "$PUID:$PGID" /app/data

exec su-exec "$user" dotnet FunkArr.dll

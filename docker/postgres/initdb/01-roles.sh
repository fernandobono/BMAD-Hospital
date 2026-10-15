#!/usr/bin/env bash
# Por qué dos roles: la app (turnos_app) no puede cambiar el esquema ni, desde la story 1.3,
# tocar la auditoría. Sólo el dueño (turnos_owner) corre migraciones. Arq. §7.5.
#
# Por qué "set -eo pipefail" y NO "set -u": si el archivo no tiene bit ejecutable, el entrypoint
# de postgres lo INCLUYE con "." en vez de ejecutarlo, y un "set -u" quedaría activo en el propio
# entrypoint y podría cortarlo con "unbound variable" (revisión I-14).
set -eo pipefail

# Falta una contraseña: cortar con un mensaje claro, no crear un rol sin clave.
: "${TURNOS_OWNER_PASSWORD:?falta TURNOS_OWNER_PASSWORD (copiá .env.ejemplo a .env)}"
: "${TURNOS_APP_PASSWORD:?falta TURNOS_APP_PASSWORD (copiá .env.ejemplo a .env)}"

# Las claves y el nombre de la base van como variables de psql (:'x' y :"x"), así una comilla en
# la contraseña no rompe el SQL. Heredoc entre comillas: bash no expande nada adentro.
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
     -v base="$POSTGRES_DB" -v clave_owner="$TURNOS_OWNER_PASSWORD" -v clave_app="$TURNOS_APP_PASSWORD" <<-'SQL'
  CREATE ROLE turnos_owner LOGIN PASSWORD :'clave_owner';
  CREATE ROLE turnos_app   LOGIN PASSWORD :'clave_app';
  -- Dueño de la base = dueño del esquema public (pg_database_owner, PostgreSQL 15+).
  ALTER DATABASE :"base" OWNER TO turnos_owner;
  GRANT CONNECT ON DATABASE :"base" TO turnos_app;
  GRANT USAGE ON SCHEMA public TO turnos_app;
  REVOKE CREATE ON SCHEMA public FROM PUBLIC;
  ALTER DEFAULT PRIVILEGES FOR ROLE turnos_owner IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO turnos_app;
  ALTER DEFAULT PRIVILEGES FOR ROLE turnos_owner IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO turnos_app;
SQL

#!/bin/sh
set -eu

psql --set=ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname postgres \
  --set=app_password="$APP_PASSWORD" <<'SQL'
CREATE ROLE os_triage_app
    LOGIN
    NOSUPERUSER
    NOCREATEDB
    NOCREATEROLE
    NOINHERIT
    PASSWORD :'app_password';

CREATE DATABASE os_triage_api
    OWNER os_triage_app
    ENCODING 'UTF8';
SQL

#!/bin/sh
set -eu

APP_PASSWORD="$(cat "$APP_PASSWORD_FILE")"

psql --set=ON_ERROR_STOP=1 --set=app_password="$APP_PASSWORD" --username "$POSTGRES_USER" --dbname postgres <<'SQL'
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

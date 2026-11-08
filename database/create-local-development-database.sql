-- Ejecuta este archivo una sola vez con una cuenta administradora de MySQL.
-- Cambia la contraseña antes de ejecutarlo y usa la misma en appsettings.Development.json.
CREATE DATABASE IF NOT EXISTS coachdesk_dev
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

CREATE USER IF NOT EXISTS 'coachdesk_app'@'localhost'
  IDENTIFIED BY 'REEMPLAZA_POR_UNA_CONTRASENA_LOCAL_SEGURA';

GRANT SELECT, INSERT, UPDATE, DELETE, CREATE, ALTER, INDEX, DROP, REFERENCES
  ON coachdesk_dev.* TO 'coachdesk_app'@'localhost';

FLUSH PRIVILEGES;


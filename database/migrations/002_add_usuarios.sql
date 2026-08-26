CREATE TABLE IF NOT EXISTS usuario (
    id_usuario SERIAL PRIMARY KEY,
    nombre VARCHAR(120) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    rol VARCHAR(20) NOT NULL,
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT chk_usuario_rol
        CHECK (rol IN ('ADMINISTRADOR', 'TRABAJADOR'))
);

INSERT INTO usuario
(nombre, email, password_hash, rol, activo)
VALUES
(
    'Administrador GIA',
    'admin@gia.local',
    'PBKDF2-SHA256.100000.MnWwfBEtYf5GX48gn8cJyA==.OFObcfFy8dehupr9pVhbCQYqUXhO1vq5TKnfUE+iZ2A=',
    'ADMINISTRADOR',
    TRUE
)
ON CONFLICT (email) DO NOTHING;

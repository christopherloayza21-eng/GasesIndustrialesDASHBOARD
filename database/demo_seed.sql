-- Datos demo para portafolio.
-- IMPORTANTE: este script limpia las tablas principales.
-- Usarlo solo en una base de datos demo, nunca en una base real de empresa.

BEGIN;

TRUNCATE TABLE
    detalle_envio_recarga,
    envio_recarga,
    movimiento_cilindro,
    detalle_pedido,
    pedido,
    cilindro,
    producto,
    cliente,
    zona,
    conductor,
    vehiculo,
    proveedor,
    usuario
RESTART IDENTITY CASCADE;

INSERT INTO zona (nombre, descripcion)
VALUES
('Parque Industrial', 'Clientes ubicados en zonas industriales de Arequipa'),
('Uchumayo', 'Talleres y clientes ubicados por Uchumayo y alrededores'),
('Cerro Colorado', 'Empresas con atencion por Cerro Colorado'),
('Cercado', 'Clientes cercanos al centro de Arequipa');

INSERT INTO cliente
(razon_social, ruc, telefono, direccion, id_zona, tipo_cliente, requiere_garantia)
VALUES
('Metalurgica Sur Demo S.A.C.', '20123456789', '999111222',
 'Av. Industrial 120, Arequipa', 1, 'FRECUENTE', FALSE),
('Taller Demo E.I.R.L.', '20987654321', '988777666',
 'Uchumayo km 5', 2, 'FRECUENTE', TRUE),
('Clinica Ocupacional Demo S.A.C.', '20600111222', '955444333',
 'Cercado de Arequipa', 4, 'EVENTUAL', TRUE),
('Constructora Andina Demo S.R.L.', '20444555666', '977123456',
 'Cerro Colorado', 3, 'NUEVO', TRUE);

INSERT INTO producto
(codigo, nombre, tipo_producto, unidad_medida, precio_referencia)
VALUES
('OX-M3', 'Oxigeno Industrial', 'GAS', 'M3', 14.30),
('CO2-KG', 'Dioxido de Carbono', 'GAS', 'KG', 8.50),
('ARG-M3', 'Argon Industrial', 'GAS', 'M3', 18.00),
('ACE-KG', 'Acetileno', 'GAS', 'KG', 25.00),
('NIT-M3', 'Nitrogeno', 'GAS', 'M3', 16.00),
('EQ-MIG-210', 'Soldadora MIG 210', 'EQUIPO', 'UND', 1350.00),
('EQ-INV-200', 'Soldadora Inverter 200A', 'EQUIPO', 'UND', 780.00),
('INS-ELEC-6011', 'Electrodo 6011 3.25 mm', 'INSUMO', 'KG', 12.50),
('INS-ALAM-08', 'Alambre MIG 0.8 mm', 'INSUMO', 'KG', 18.90),
('SERV-RECARGA', 'Servicio de recarga de cilindro', 'SERVICIO', 'UND', 35.00);

INSERT INTO cilindro
(codigo_cilindro, id_producto, capacidad, propietario_tipo, id_cliente_propietario, estado_actual, ubicacion_actual, activo)
VALUES
('OX-AQP-1001', 1, 10, 'EMPRESA', NULL, 'LLENO_ALMACEN', 'Almacen llenos', TRUE),
('OX-AQP-1002', 1, 10, 'EMPRESA', NULL, 'EN_CLIENTE', 'Taller Demo E.I.R.L.', TRUE),
('OX-AQP-1003', 1, 6, 'EMPRESA', NULL, 'VACIO_ALMACEN', 'Almacen vacios', TRUE),
('CO2-AQP-2001', 2, 25, 'EMPRESA', NULL, 'LLENO_ALMACEN', 'Almacen llenos', TRUE),
('CO2-AQP-2002', 2, 25, 'EMPRESA', NULL, 'EN_CLIENTE', 'Metalurgica Sur Demo S.A.C.', TRUE),
('ARG-AQP-3001', 3, 10, 'EMPRESA', NULL, 'LLENO_ALMACEN', 'Almacen llenos', TRUE),
('ACE-AQP-4001', 4, 8, 'EMPRESA', NULL, 'EN_PROVEEDOR', 'Proveedor Demo Gases S.A.C.', TRUE),
('NIT-AQP-5001', 5, 10, 'EMPRESA', NULL, 'BAJA', 'Fuera de servicio', FALSE);

INSERT INTO conductor (nombre, telefono)
VALUES
('Luis Mendoza Demo', '999111111'),
('Carlos Quispe Demo', '999222222'),
('Rosa Arias Demo', '999333444');

INSERT INTO vehiculo (placa, descripcion)
VALUES
('DEM-001', 'Camioneta de reparto urbano'),
('DEM-002', 'Unidad para reparto industrial'),
('DEM-003', 'Unidad de apoyo para provincias');

INSERT INTO proveedor
(razon_social, ruc, telefono, direccion)
VALUES
('Proveedor Demo Gases S.A.C.', '20111222333', '999888777', 'Arequipa'),
('Planta Recargadora Demo S.R.L.', '20555666777', '988555444', 'Cerro Colorado');

INSERT INTO pedido
(id_cliente, direccion_entrega, id_zona, id_conductor, id_vehiculo, estado_pedido, observaciones)
VALUES
(2, 'Uchumayo km 5', 2, 1, 1, 'PENDIENTE', 'Cliente solicita entrega durante la manana'),
(1, 'Av. Industrial 120, Arequipa', 1, 2, 2, 'ASIGNADO', 'Pedido programado para taller de mantenimiento'),
(3, 'Cercado de Arequipa', 4, 3, 1, 'ENTREGADO', 'Entrega completada para uso tecnico');

INSERT INTO detalle_pedido
(id_pedido, id_producto, cantidad, precio_unitario, subtotal)
VALUES
(1, 1, 15, 14.30, 214.50),
(1, 8, 5, 12.50, 62.50),
(2, 2, 10, 8.50, 85.00),
(2, 6, 1, 1350.00, 1350.00),
(3, 1, 6, 14.30, 85.80),
(3, 10, 1, 35.00, 35.00);

INSERT INTO movimiento_cilindro
(id_cilindro, id_pedido, tipo_movimiento, fecha_movimiento, id_cliente, id_conductor, id_vehiculo, observacion)
VALUES
(2, 1, 'SALIDA_CLIENTE', '2026-08-20 09:15:00', 2, 1, 1, 'Cilindro entregado al taller para trabajo de soldadura'),
(5, 2, 'SALIDA_CLIENTE', '2026-08-20 10:30:00', 1, 2, 2, 'Cilindro CO2 entregado a cliente industrial'),
(3, NULL, 'RETORNO_CLIENTE', '2026-08-21 16:40:00', 2, 1, 1, 'Cilindro retornado vacio al almacen'),
(7, NULL, 'ENVIO_RECARGA', '2026-08-22 08:20:00', NULL, 2, 2, 'Cilindro enviado a proveedor para recarga');

INSERT INTO envio_recarga
(id_proveedor, fecha_envio, numero_guia, estado, observaciones)
VALUES
(1, '2026-08-22 08:20:00', 'GUIA-DEMO-001', 'ENVIADO', 'Recarga pendiente para acetileno');

INSERT INTO detalle_envio_recarga
(id_envio, id_cilindro, estado_retorno, observacion)
VALUES
(1, 7, 'PENDIENTE', 'Pendiente de retorno desde proveedor');

INSERT INTO usuario
(nombre, email, password_hash, rol, activo)
VALUES
(
    'Administrador GIA Demo',
    'admin@gia.local',
    'PBKDF2-SHA256.100000.MnWwfBEtYf5GX48gn8cJyA==.OFObcfFy8dehupr9pVhbCQYqUXhO1vq5TKnfUE+iZ2A=',
    'ADMINISTRADOR',
    TRUE
),
(
    'Trabajador GIA Demo',
    'trabajador@gia.local',
    'PBKDF2-SHA256.100000.MnWwfBEtYf5GX48gn8cJyA==.OFObcfFy8dehupr9pVhbCQYqUXhO1vq5TKnfUE+iZ2A=',
    'TRABAJADOR',
    TRUE
);

COMMIT;

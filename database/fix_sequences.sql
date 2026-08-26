-- Repara las secuencias SERIAL si se insertaron datos manualmente con IDs fijos.
-- Usalo solo si al crear registros PostgreSQL muestra errores de clave duplicada.

SELECT setval(pg_get_serial_sequence('zona', 'id_zona'), COALESCE((SELECT MAX(id_zona) FROM zona), 0) + 1, false);
SELECT setval(pg_get_serial_sequence('cliente', 'id_cliente'), COALESCE((SELECT MAX(id_cliente) FROM cliente), 0) + 1, false);
SELECT setval(pg_get_serial_sequence('producto', 'id_producto'), COALESCE((SELECT MAX(id_producto) FROM producto), 0) + 1, false);
SELECT setval(pg_get_serial_sequence('cilindro', 'id_cilindro'), COALESCE((SELECT MAX(id_cilindro) FROM cilindro), 0) + 1, false);
SELECT setval(pg_get_serial_sequence('conductor', 'id_conductor'), COALESCE((SELECT MAX(id_conductor) FROM conductor), 0) + 1, false);
SELECT setval(pg_get_serial_sequence('vehiculo', 'id_vehiculo'), COALESCE((SELECT MAX(id_vehiculo) FROM vehiculo), 0) + 1, false);
SELECT setval(pg_get_serial_sequence('pedido', 'id_pedido'), COALESCE((SELECT MAX(id_pedido) FROM pedido), 0) + 1, false);
SELECT setval(pg_get_serial_sequence('detalle_pedido', 'id_detalle'), COALESCE((SELECT MAX(id_detalle) FROM detalle_pedido), 0) + 1, false);
SELECT setval(pg_get_serial_sequence('movimiento_cilindro', 'id_movimiento'), COALESCE((SELECT MAX(id_movimiento) FROM movimiento_cilindro), 0) + 1, false);
SELECT setval(pg_get_serial_sequence('proveedor', 'id_proveedor'), COALESCE((SELECT MAX(id_proveedor) FROM proveedor), 0) + 1, false);
SELECT setval(pg_get_serial_sequence('envio_recarga', 'id_envio'), COALESCE((SELECT MAX(id_envio) FROM envio_recarga), 0) + 1, false);
SELECT setval(pg_get_serial_sequence('detalle_envio_recarga', 'id_detalle_envio'), COALESCE((SELECT MAX(id_detalle_envio) FROM detalle_envio_recarga), 0) + 1, false);

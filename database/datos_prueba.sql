-- =========================================================
-- Buffet Express · Grupo 4
-- Pedidos de ejemplo para probar el panel del buffet (TC-05).
-- Son los mismos del boceto del informe y quedan con la fecha de hoy.
-- Correr una sola vez, después de script.sql:
--   mysql -u root -p < database/datos_prueba.sql
-- (No descuenta stock: son solo para ver el panel.)
-- =========================================================

USE buffet_express;

-- Pedidos (dos para el recreo de las 10:10 y uno para el de las 12:15)
INSERT INTO pedidos (codigo_retiro, nombre, curso, recreo, total, estado, fecha) VALUES
  ('4827', 'Juan Pérez', '5.º A', '10:10', 3500, 'Pendiente', NOW()),
  ('3150', 'Ana Gómez', '4.º B', '10:10', 3700, 'Listo para retirar', NOW()),
  ('9071', 'Lucas Díaz', '3.º A', '12:15', 5000, 'Pendiente', NOW());

-- Productos de cada pedido (busca los id por código y por nombre)
INSERT INTO pedido_items (pedido_id, producto_id, cantidad, precio_unitario)
SELECT pe.id, pr.id, 1, pr.precio
FROM pedidos pe, productos pr
WHERE pe.codigo_retiro = '4827' AND pr.nombre = 'Sándwich de milanesa';

INSERT INTO pedido_items (pedido_id, producto_id, cantidad, precio_unitario)
SELECT pe.id, pr.id, 1, pr.precio
FROM pedidos pe, productos pr
WHERE pe.codigo_retiro = '3150' AND pr.nombre IN ('Tostado', 'Agua 500 ml');

INSERT INTO pedido_items (pedido_id, producto_id, cantidad, precio_unitario)
SELECT pe.id, pr.id, 2, pr.precio
FROM pedidos pe, productos pr
WHERE pe.codigo_retiro = '9071' AND pr.nombre = 'Tostado';

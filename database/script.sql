-- =========================================================
-- Buffet Express · Grupo 4
-- Crea la base de datos, las tablas y el menú inicial.
-- La app lo ejecuta sola al arrancar si la base no existe.
-- También se puede correr a mano:  mysql -u root -p < database/script.sql
-- =========================================================

CREATE DATABASE IF NOT EXISTS buffet_express
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE buffet_express;

-- Productos del menú del día
CREATE TABLE IF NOT EXISTS productos (
  id     INT PRIMARY KEY AUTO_INCREMENT,
  nombre VARCHAR(60) NOT NULL UNIQUE,
  precio INT NOT NULL,
  stock  INT NOT NULL DEFAULT 0
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

-- Pedidos de los alumnos
-- estado: 'Pendiente' -> 'Listo para retirar' -> 'Entregado'
-- recreo: '10:10' o '12:15'
CREATE TABLE IF NOT EXISTS pedidos (
  id            INT PRIMARY KEY AUTO_INCREMENT,
  codigo_retiro CHAR(4) NOT NULL UNIQUE,
  nombre        VARCHAR(100) NOT NULL,
  curso         VARCHAR(20) NOT NULL,
  recreo        VARCHAR(5) NOT NULL,
  total         INT NOT NULL,
  estado        VARCHAR(30) NOT NULL DEFAULT 'Pendiente',
  fecha         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

-- Productos de cada pedido (con el precio que tenían al pedir)
CREATE TABLE IF NOT EXISTS pedido_items (
  id              INT PRIMARY KEY AUTO_INCREMENT,
  pedido_id       INT NOT NULL,
  producto_id     INT NOT NULL,
  cantidad        INT NOT NULL,
  precio_unitario INT NOT NULL,
  FOREIGN KEY (pedido_id) REFERENCES pedidos (id),
  FOREIGN KEY (producto_id) REFERENCES productos (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

-- Menú inicial (INSERT IGNORE: si el producto ya existe, no lo repite)
INSERT IGNORE INTO productos (nombre, precio, stock) VALUES
  ('Tostado', 2500, 40),
  ('Sándwich de milanesa', 3500, 30),
  ('Agua 500 ml', 1200, 60),
  ('Alfajor', 1500, 50),
  ('Jugo de naranja', 1800, 40);

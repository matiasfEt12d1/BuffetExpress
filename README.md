# Buffet Express

**Pedí antes. Retirá sin fila.**

Aplicación para que los alumnos de la E.T. N.º 12 D.E. 1 pidan la comida del buffet antes del recreo y la retiren con un código, sin hacer fila.

Trabajo práctico integrador de Proyecto Informático II y Economía (5.º año) · Grupo 4. Es una **simulación que corre en forma local** (no está publicada en internet).

## Integrantes

| Integrante | GitHub | Rol (Scrum) |
|---|---|---|
| Matías Flor | [matiasfEt12d1](https://github.com/matiasfEt12d1) | Scrum Master y Desarrollador |
| Daniel Contreras | [danbrih](https://github.com/danbrih) | Desarrollador |
| Agustín Gutierrez | [Agus2555](https://github.com/Agus2555) | Product Owner y QA |

## Cómo funciona

1. **Catálogo**: el alumno ve el menú del día y toca "Agregar".
2. **Mi pedido**: revisa lo que eligió y el recreo en el que lo retira (10:10 o 12:15).
3. **Tus datos**: escribe nombre, apellido, curso y división, y confirma.
4. **Ticket de retiro**: recibe un código de 4 dígitos para mostrar en el buffet.

El buffet ve los pedidos de hoy en un **panel** ordenado por recreo y los marca como **Listo para retirar** y después **Entregado**. El alumno ve el cambio en su ticket al tocar "Actualizar".

El pago se hace en el buffet al retirar. La comisión de $150 la cobra el buffet junto con cada compra reservada, así que no se suma al total en la app.

## Arquitectura

```
Navegador (HTML + CSS, formularios)  ──>  Servidor C# (ASP.NET Core Minimal API)  ──>  MySQL (buffet_express)
                                     <──  páginas HTML armadas con plantillas
Cliente de la API (pedidos.http)     ──>  API en JSON (/api/menu, /api/pedidos)
```

- **Páginas HTML y CSS, sin JavaScript**: cada pantalla es un formulario (`<form method="post">`) que se envía al servidor. El servidor responde con la página siguiente o redirige.
- **Servidor C#** (.NET 10, ASP.NET Core Minimal API): arma el HTML leyendo plantillas `.html` y reemplazando marcadores como `{{CODIGO}}`. El pedido que el alumno va armando se guarda en la **sesión del servidor** hasta que lo confirma.
- **MySQL**: guarda productos, pedidos y los productos de cada pedido. Confirmar un pedido inserta el pedido, sus ítems y descuenta el stock en una **transacción**.
- **API en JSON** (prueba de concepto): `GET /api/menu` y `POST /api/pedidos`.
- Único paquete NuGet: **MySqlConnector**.

### Estructura de carpetas

```
BuffetExpress.sln
BuffetExpress/
  Program.cs                    arranque, sesión y registro de rutas
  appsettings.json              puerto (http://localhost:5000) y logs
  appsettings.ejemplo.json      cadena de conexión sin contraseña
  Datos/Conexion.cs             abre conexiones y crea la base si no existe
  Datos/PedidosRepositorio.cs   consultas SQL
  Models/                       Producto, Pedido, ItemPedido, PedidoNuevo, PedidoRespuesta
  Rutas/RutasApi.cs             /api/menu y /api/pedidos (JSON)
  Rutas/RutasAlumno.cs          catálogo, pedido, datos, confirmar y ticket
  Rutas/RutasBuffet.cs          panel y cambios de estado
  Plantillas/Plantilla.cs       lee un .html y reemplaza los marcadores
  Plantillas/*.html             marco, catalogo, pedido, datos, ticket, buffet y error
  wwwroot/css/                  base.css, alumno.css y buffet.css
  wwwroot/img/                  logo y dibujos SVG de los productos
  pedidos.http                  pruebas de la API
database/script.sql             crea la base, las tablas y el menú inicial
database/datos_prueba.sql       pedidos de ejemplo para probar el panel
```

### Base de datos

| Tabla | Columnas |
|---|---|
| `productos` | id, nombre, precio, stock |
| `pedidos` | id, codigo_retiro (4 dígitos, único), nombre, curso, recreo, total, estado, fecha |
| `pedido_items` | id, pedido_id, producto_id, cantidad, precio_unitario |

## Cómo ejecutarlo

### Requisitos
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- MySQL 8 (servidor) con el usuario `root` en `localhost:3306`

No hace falta crear la base a mano: **al arrancar, si `buffet_express` no existe, la app ejecuta `database/script.sql`**.

### Windows

1. Clonar el repositorio y entrar a la carpeta:
   ```powershell
   git clone https://github.com/matiasfEt12d1/BuffetExpress.git
   cd BuffetExpress
   ```
2. Copiar la configuración de ejemplo y poner la contraseña de MySQL en `Password=` (este archivo no se sube a GitHub):
   ```powershell
   Copy-Item BuffetExpress\appsettings.ejemplo.json BuffetExpress\appsettings.Development.json
   notepad BuffetExpress\appsettings.Development.json
   ```
3. Ejecutar:
   ```powershell
   dotnet run --project BuffetExpress
   ```
4. Abrir `http://localhost:5000` (vista del alumno) y `http://localhost:5000/buffet` (panel del buffet).

### Linux

1. Clonar el repositorio y entrar a la carpeta:
   ```bash
   git clone https://github.com/matiasfEt12d1/BuffetExpress.git
   cd BuffetExpress
   ```
2. Copiar la configuración y poner la contraseña:
   ```bash
   cp BuffetExpress/appsettings.ejemplo.json BuffetExpress/appsettings.Development.json
   nano BuffetExpress/appsettings.Development.json
   ```
   O, en vez del archivo, usar una variable de entorno:
   ```bash
   export ConnectionStrings__BuffetExpress="Server=localhost;Port=3306;Database=buffet_express;User ID=root;Password=TU_CONTRASEÑA;CharSet=utf8mb4"
   ```
3. Ejecutar:
   ```bash
   dotnet run --project BuffetExpress
   ```
4. Abrir `http://localhost:5000` y `http://localhost:5000/buffet`.

### Pedidos de ejemplo para el panel (opcional)

```
mysql -u root -p < database/datos_prueba.sql
```
(En Windows también se puede abrir el archivo en MySQL Workbench y ejecutarlo.) Carga tres pedidos con la fecha de hoy: dos para las 10:10 y uno para las 12:15.

## Probar la API (prueba de concepto)

Con la app corriendo, abrir `BuffetExpress/pedidos.http` en Visual Studio, Rider o VS Code (extensión REST Client) y ejecutar cada pedido. También se puede usar `curl`:

```bash
curl http://localhost:5000/api/menu
curl -X POST http://localhost:5000/api/pedidos -H "Content-Type: application/json" \
  -d '{"nombre":"Juan Pérez","curso":"5.º A","recreo":"10:10","productos":[{"nombre":"Sándwich de milanesa","cantidad":1,"precio":3500}]}'
```

Respuesta (201):
```json
{ "id_pedido": 15, "codigo_retiro": "8842", "total": 3500, "recreo": "10:10", "estado": "Pendiente", "fecha": "24/11/2026" }
```

- Si falta algún dato o viene vacío, responde **400** con `{ "error": "..." }`.
- El precio que manda el cliente se ignora: el total se calcula con los precios de la base.

## Pruebas

| ID | Prueba | Resultado esperado |
|---|---|---|
| TC-01 | Tocar "Agregar" en el tostado de $2.500 | Aparece en el pedido y el total muestra $2.500 |
| TC-02 | 1 sándwich de milanesa, nombre, curso y recreo 10:10, "Confirmar pedido" | Ticket con código de 4 dígitos, total $3.500, recreo 10:10 y estado Pendiente |
| TC-03 | "Confirmar pedido" sin nombre | No deja confirmar y avisa que falta el nombre (la API responde 400) |
| TC-04 | Avanzar sin elegir recreo | No deja avanzar y pide elegir 10:10 o 12:15 |
| TC-05 | Panel con pedidos de 10:10 y 12:15 | Primero los de 10:10; cada tarjeta con código, nombre, curso y productos |
| TC-06 | "Listo para retirar" en un pedido | Cambia en el panel y en el ticket del alumno (al tocar "Actualizar") |

Las seis pruebas pasaron. Los resultados detallados y los tiempos de la prueba de concepto están en el informe.

## Forma de trabajo

- Ramas encadenadas: cada una sale de la anterior y todas las Pull Requests van a `develop`:
  ```
  main ── develop ── feature/poc-pedidos ── feature/vista-alumno ── feature/panel-buffet ── fix/pruebas ── docs/readme
  ```
- `main` y `develop` están protegidas: solo se cambian con Pull Request y 1 aprobación.
- Las Pull Requests se unen en orden. Al final, `develop` se une a `main`.
- Mensajes de commit en español: verbo en presente + qué + historia o tarea (por ejemplo, `Agrega catálogo del menú (HU-01)`).
- El tablero de tareas está documentado en el informe (no se usó Trello por indicación del profesor).

using BuffetExpress.Models;
using MySqlConnector;

namespace BuffetExpress.Datos;

// Todas las consultas SQL de la aplicación.
// Se usan parámetros (@nombre) para que no se pueda meter SQL desde los formularios.
public static class PedidosRepositorio
{
    // ---------- Menú ----------

    // Todos los productos, en el orden en que se cargaron
    public static List<Producto> ObtenerMenu()
    {
        var productos = new List<Producto>();
        using var conexion = Conexion.Abrir();
        using var comando = new MySqlCommand(
            "SELECT id, nombre, precio, stock FROM productos ORDER BY id", conexion);
        using var lector = comando.ExecuteReader();
        while (lector.Read())
        {
            productos.Add(LeerProducto(lector));
        }
        return productos;
    }

    // Un producto por su id (o null si no existe)
    public static Producto? ObtenerProducto(int id)
    {
        using var conexion = Conexion.Abrir();
        using var comando = new MySqlCommand(
            "SELECT id, nombre, precio, stock FROM productos WHERE id = @id", conexion);
        comando.Parameters.AddWithValue("@id", id);
        using var lector = comando.ExecuteReader();
        if (lector.Read())
        {
            return LeerProducto(lector);
        }
        return null;
    }

    private static Producto LeerProducto(MySqlDataReader lector)
    {
        return new Producto
        {
            Id = lector.GetInt32("id"),
            Nombre = lector.GetString("nombre"),
            Precio = lector.GetInt32("precio"),
            Stock = lector.GetInt32("stock")
        };
    }

    // ---------- Guardar un pedido ----------

    // Guarda el pedido, sus productos y descuenta el stock, todo en una transacción:
    // si algo falla, no se guarda nada. Devuelve el id del pedido nuevo.
    // Si falta stock o un producto no existe, lanza un error con un mensaje claro.
    // El total se calcula con los precios de la base (no con los que manda el cliente).
    public static int GuardarPedido(PedidoNuevo pedido)
    {
        // 1) Juntamos los productos repetidos (por si vienen dos veces)
        var cantidades = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in pedido.Productos!)
        {
            string nombre = item.Nombre.Trim();
            if (cantidades.ContainsKey(nombre))
                cantidades[nombre] += item.Cantidad;
            else
                cantidades[nombre] = item.Cantidad;
        }

        using var conexion = Conexion.Abrir();
        using var transaccion = conexion.BeginTransaction();
        try
        {
            // 2) Buscamos cada producto, revisamos el stock y calculamos el total
            var items = new List<ItemPedido>();
            int total = 0;
            foreach (var par in cantidades)
            {
                using var buscar = new MySqlCommand(
                    "SELECT id, nombre, precio, stock FROM productos WHERE nombre = @nombre FOR UPDATE",
                    conexion, transaccion);
                buscar.Parameters.AddWithValue("@nombre", par.Key);

                Producto? producto = null;
                using (var lector = buscar.ExecuteReader())
                {
                    if (lector.Read()) producto = LeerProducto(lector);
                }

                if (producto == null)
                    throw new InvalidOperationException("El producto " + par.Key + " no está en el menú.");
                if (producto.Stock < par.Value)
                    throw new InvalidOperationException("No queda stock suficiente de " + producto.Nombre
                        + " (quedan " + producto.Stock + ").");

                items.Add(new ItemPedido
                {
                    ProductoId = producto.Id,
                    Nombre = producto.Nombre,
                    Cantidad = par.Value,
                    Precio = producto.Precio
                });
                total += par.Value * producto.Precio;
            }

            // 3) Código de retiro de 4 dígitos que no esté repetido
            string codigo = GenerarCodigo(conexion, transaccion);

            // 4) Guardamos el pedido
            using var insertar = new MySqlCommand(
                "INSERT INTO pedidos (codigo_retiro, nombre, curso, recreo, total, estado, fecha) " +
                "VALUES (@codigo, @nombre, @curso, @recreo, @total, @estado, NOW())",
                conexion, transaccion);
            insertar.Parameters.AddWithValue("@codigo", codigo);
            insertar.Parameters.AddWithValue("@nombre", pedido.Nombre!.Trim());
            insertar.Parameters.AddWithValue("@curso", pedido.Curso!.Trim());
            insertar.Parameters.AddWithValue("@recreo", pedido.Recreo);
            insertar.Parameters.AddWithValue("@total", total);
            insertar.Parameters.AddWithValue("@estado", Pedido.Pendiente);
            insertar.ExecuteNonQuery();
            int pedidoId = (int)insertar.LastInsertedId;

            // 5) Guardamos cada producto del pedido y descontamos el stock
            foreach (var item in items)
            {
                using var insertarItem = new MySqlCommand(
                    "INSERT INTO pedido_items (pedido_id, producto_id, cantidad, precio_unitario) " +
                    "VALUES (@pedido, @producto, @cantidad, @precio)",
                    conexion, transaccion);
                insertarItem.Parameters.AddWithValue("@pedido", pedidoId);
                insertarItem.Parameters.AddWithValue("@producto", item.ProductoId);
                insertarItem.Parameters.AddWithValue("@cantidad", item.Cantidad);
                insertarItem.Parameters.AddWithValue("@precio", item.Precio);
                insertarItem.ExecuteNonQuery();

                using var descontar = new MySqlCommand(
                    "UPDATE productos SET stock = stock - @cantidad WHERE id = @producto",
                    conexion, transaccion);
                descontar.Parameters.AddWithValue("@cantidad", item.Cantidad);
                descontar.Parameters.AddWithValue("@producto", item.ProductoId);
                descontar.ExecuteNonQuery();
            }

            transaccion.Commit();
            return pedidoId;
        }
        catch
        {
            transaccion.Rollback();
            throw;
        }
    }

    // Elige 4 dígitos al azar (por ejemplo "0427") y reintenta si ya existen
    private static string GenerarCodigo(MySqlConnection conexion, MySqlTransaction transaccion)
    {
        for (int intento = 0; intento < 100; intento++)
        {
            string codigo = Random.Shared.Next(0, 10000).ToString("D4");

            using var existe = new MySqlCommand(
                "SELECT COUNT(*) FROM pedidos WHERE codigo_retiro = @codigo", conexion, transaccion);
            existe.Parameters.AddWithValue("@codigo", codigo);
            if (Convert.ToInt64(existe.ExecuteScalar()) == 0)
            {
                return codigo;
            }
        }
        throw new InvalidOperationException("No se pudo generar un código de retiro. Probá de nuevo.");
    }

    // ---------- Leer pedidos ----------

    // Un pedido con sus productos (o null si no existe)
    public static Pedido? ObtenerPedido(int id)
    {
        using var conexion = Conexion.Abrir();
        Pedido? pedido = null;
        using (var comando = new MySqlCommand(
            "SELECT id, codigo_retiro, nombre, curso, recreo, total, estado, fecha FROM pedidos WHERE id = @id",
            conexion))
        {
            comando.Parameters.AddWithValue("@id", id);
            using var lector = comando.ExecuteReader();
            if (lector.Read())
            {
                pedido = LeerPedido(lector);
            }
        }

        if (pedido != null)
        {
            pedido.Items = ObtenerItems(conexion, pedido.Id);
        }
        return pedido;
    }

    // Pedidos de hoy, ordenados por horario de retiro (primero 10:10, después 12:15)
    // y, dentro de cada recreo, por orden de llegada.
    public static List<Pedido> ObtenerPedidosDeHoy()
    {
        var pedidos = new List<Pedido>();
        using var conexion = Conexion.Abrir();
        using (var comando = new MySqlCommand(
            "SELECT id, codigo_retiro, nombre, curso, recreo, total, estado, fecha FROM pedidos " +
            "WHERE DATE(fecha) = CURDATE() ORDER BY recreo, fecha, id", conexion))
        {
            using var lector = comando.ExecuteReader();
            while (lector.Read())
            {
                pedidos.Add(LeerPedido(lector));
            }
        }

        // Los productos de cada pedido (cuando ya cerramos el lector anterior)
        foreach (var pedido in pedidos)
        {
            pedido.Items = ObtenerItems(conexion, pedido.Id);
        }
        return pedidos;
    }

    // ---------- Cambiar el estado ----------

    // Pasa el pedido de un estado al siguiente (Pendiente -> Listo -> Entregado).
    // Solo cambia si el pedido está en el estado anterior que corresponde,
    // así un doble clic no lo saltea. Devuelve true si cambió.
    public static bool CambiarEstado(int id, string estadoAnterior, string estadoNuevo)
    {
        using var conexion = Conexion.Abrir();
        using var comando = new MySqlCommand(
            "UPDATE pedidos SET estado = @nuevo WHERE id = @id AND estado = @anterior", conexion);
        comando.Parameters.AddWithValue("@nuevo", estadoNuevo);
        comando.Parameters.AddWithValue("@id", id);
        comando.Parameters.AddWithValue("@anterior", estadoAnterior);
        return comando.ExecuteNonQuery() > 0;
    }

    private static Pedido LeerPedido(MySqlDataReader lector)
    {
        return new Pedido
        {
            Id = lector.GetInt32("id"),
            CodigoRetiro = lector.GetString("codigo_retiro"),
            Nombre = lector.GetString("nombre"),
            Curso = lector.GetString("curso"),
            Recreo = lector.GetString("recreo"),
            Total = lector.GetInt32("total"),
            Estado = lector.GetString("estado"),
            Fecha = lector.GetDateTime("fecha")
        };
    }

    // Productos de un pedido, con el nombre de cada uno
    private static List<ItemPedido> ObtenerItems(MySqlConnection conexion, int pedidoId)
    {
        var items = new List<ItemPedido>();
        using var comando = new MySqlCommand(
            "SELECT pi.producto_id, p.nombre, pi.cantidad, pi.precio_unitario " +
            "FROM pedido_items pi JOIN productos p ON p.id = pi.producto_id " +
            "WHERE pi.pedido_id = @pedido ORDER BY pi.id", conexion);
        comando.Parameters.AddWithValue("@pedido", pedidoId);
        using var lector = comando.ExecuteReader();
        while (lector.Read())
        {
            items.Add(new ItemPedido
            {
                ProductoId = lector.GetInt32("producto_id"),
                Nombre = lector.GetString("nombre"),
                Cantidad = lector.GetInt32("cantidad"),
                Precio = lector.GetInt32("precio_unitario")
            });
        }
        return items;
    }
}

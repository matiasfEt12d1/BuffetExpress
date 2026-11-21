using BuffetExpress.Datos;
using BuffetExpress.Models;

namespace BuffetExpress.Rutas;

// API en JSON (prueba de concepto, T-15)
//   GET  /api/menu     -> lista de productos
//   POST /api/pedidos  -> recibe un pedido y devuelve el código de retiro
public static class RutasApi
{
    public static void Registrar(WebApplication app)
    {
        app.MapGet("/api/menu", () =>
        {
            return Results.Ok(PedidosRepositorio.ObtenerMenu());
        });

        app.MapPost("/api/pedidos", (PedidoNuevo? pedido) =>
        {
            // Si no mandaron nada, respondemos 400
            if (pedido == null)
            {
                return Results.BadRequest(new { error = "No llegaron los datos del pedido." });
            }

            // Datos faltantes o vacíos -> 400 con el mensaje
            string error = pedido.Validar();
            if (error != "")
            {
                return Results.BadRequest(new { error = error });
            }

            int id;
            try
            {
                id = PedidosRepositorio.GuardarPedido(pedido);
            }
            catch (InvalidOperationException ex)
            {
                // Producto que no existe o sin stock
                return Results.BadRequest(new { error = ex.Message });
            }

            Pedido guardado = PedidosRepositorio.ObtenerPedido(id)!;
            var respuesta = new PedidoRespuesta
            {
                IdPedido = guardado.Id,
                CodigoRetiro = guardado.CodigoRetiro,
                Total = guardado.Total,
                Recreo = guardado.Recreo,
                Estado = guardado.Estado,
                Fecha = guardado.Fecha.ToString("dd/MM/yyyy")
            };
            return Results.Json(respuesta, statusCode: 201);
        });
    }
}

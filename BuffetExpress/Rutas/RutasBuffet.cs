using BuffetExpress.Datos;
using BuffetExpress.Models;
using BuffetExpress.Plantillas;

namespace BuffetExpress.Rutas;

// Panel del buffet (pensado para una tablet o la computadora del buffet)
public static class RutasBuffet
{
    public static void Registrar(WebApplication app)
    {
        app.MapGet("/buffet", MostrarPanel);
    }

    private static IResult MostrarPanel()
    {
        List<Pedido> pedidos = PedidosRepositorio.ObtenerPedidosDeHoy();

        // Separamos los pedidos por recreo y contamos cuántos hay de cada estado
        string tarjetas1010 = "";
        string tarjetas1215 = "";
        int cantidad1010 = 0;
        int cantidad1215 = 0;
        int pendientes = 0;
        int listos = 0;
        int entregados = 0;

        foreach (var pedido in pedidos)
        {
            if (pedido.Estado == Pedido.Pendiente) pendientes++;
            if (pedido.Estado == Pedido.Listo) listos++;
            if (pedido.Estado == Pedido.Entregado) entregados++;

            if (pedido.Recreo == PedidoNuevo.Recreo1)
            {
                tarjetas1010 += Tarjeta(pedido);
                cantidad1010++;
            }
            else
            {
                tarjetas1215 += Tarjeta(pedido);
                cantidad1215++;
            }
        }

        string sinPedidos = "<p class=\"sin-pedidos\">Todavía no hay pedidos para este recreo.</p>";
        if (tarjetas1010 == "") tarjetas1010 = sinPedidos;
        if (tarjetas1215 == "") tarjetas1215 = sinPedidos;

        var marcadores = new Dictionary<string, string>
        {
            ["FECHA"] = DateTime.Now.ToString("dd/MM/yyyy"),
            ["PENDIENTES"] = pendientes.ToString(),
            ["LISTOS"] = listos.ToString(),
            ["ENTREGADOS"] = entregados.ToString(),
            ["CANTIDAD_1010"] = TextoCantidad(cantidad1010),
            ["CANTIDAD_1215"] = TextoCantidad(cantidad1215),
            ["PEDIDOS_1010"] = tarjetas1010,
            ["PEDIDOS_1215"] = tarjetas1215
        };

        // La página se recarga sola cada 30 segundos (es HTML, no JavaScript)
        string recarga = "<meta http-equiv=\"refresh\" content=\"30\">";
        return Plantilla.Pagina("Panel del buffet", "buffet", "buffet.html", marcadores, 200, recarga);
    }

    private static string TextoCantidad(int cantidad)
    {
        if (cantidad == 1) return "1 pedido";
        return cantidad + " pedidos";
    }

    // Tarjeta de un pedido: código, nombre, curso, productos y estado
    private static string Tarjeta(Pedido pedido)
    {
        string clase = "pedido";
        if (pedido.Estado == Pedido.Entregado) clase = "pedido pedido-entregado";

        return "<article class=\"" + clase + "\">"
             + "<h3>Código <span class=\"pedido-codigo\">" + Plantilla.Texto(pedido.CodigoRetiro) + "</span></h3>"
             + "<p class=\"pedido-alumno\">" + Plantilla.Texto(pedido.Nombre) + " · " + Plantilla.Texto(pedido.Curso) + "</p>"
             + "<p class=\"pedido-items\">" + Plantilla.Texto(pedido.ResumenItems()) + "</p>"
             + "<p class=\"pedido-total\">Cobrar " + Plantilla.Pesos(pedido.Total) + " al retirar</p>"
             + "<div class=\"pedido-pie\">"
             + Plantilla.Etiqueta(pedido.Estado)
             + "</div>"
             + "</article>";
    }
}

using System.Text.Json;
using BuffetExpress.Datos;
using BuffetExpress.Models;
using BuffetExpress.Plantillas;

namespace BuffetExpress.Rutas;

// Pantallas del alumno (vista pensada para el celular).
// El pedido que el alumno va armando se guarda en la sesión del servidor
// hasta que lo confirma. No se usa JavaScript: todo son formularios.
public static class RutasAlumno
{
    public static void Registrar(WebApplication app)
    {
        // Paso 1: catálogo
        app.MapGet("/", MostrarCatalogo);
        app.MapPost("/pedido/agregar", (Delegate)AgregarProducto);

        // Paso 2: mi pedido y elección del recreo
        app.MapGet("/pedido", MostrarPedido);
        app.MapPost("/pedido/quitar", (Delegate)QuitarProducto);
        app.MapPost("/pedido/recreo", (Delegate)ElegirRecreo);
    }

    // ================= Paso 1: catálogo =================

    private static IResult MostrarCatalogo(HttpContext contexto)
    {
        PedidoNuevo pedido = LeerPedidoEnCurso(contexto);
        List<Producto> menu = PedidosRepositorio.ObtenerMenu();

        // Avisos que llegan en la dirección, por ejemplo /?aviso=stock
        string aviso = "";
        string codigoAviso = contexto.Request.Query["aviso"].ToString();
        if (codigoAviso == "vacio") aviso = "Tu pedido está vacío. Agregá algo del menú.";
        if (codigoAviso == "stock") aviso = "No queda más stock de ese producto.";
        if (codigoAviso == "maximo") aviso = "Podés pedir hasta 20 unidades de cada producto.";

        // Una tarjeta por producto
        string tarjetas = "";
        foreach (var producto in menu)
        {
            int enPedido = CantidadEnPedido(pedido, producto.Nombre);

            string boton;
            if (producto.Stock > enPedido)
            {
                boton = "<form method=\"post\" action=\"/pedido/agregar\">"
                      + "<input type=\"hidden\" name=\"id\" value=\"" + producto.Id + "\">"
                      + "<button type=\"submit\" class=\"boton boton-chico\">Agregar</button>"
                      + "</form>";
            }
            else
            {
                boton = "<button type=\"button\" class=\"boton boton-chico\" disabled>Sin stock</button>";
            }

            string marca = "";
            if (enPedido > 0)
            {
                marca = "<span class=\"en-pedido\">" + enPedido + " en tu pedido</span>";
            }

            tarjetas += "<article class=\"producto\" id=\"producto-" + producto.Id + "\">"
                      + "<img src=\"/img/" + producto.Imagen + "\" alt=\"\" class=\"producto-dibujo\" width=\"96\" height=\"96\">"
                      + "<div class=\"producto-info\">"
                      + "<h2>" + Plantilla.Texto(producto.Nombre) + "</h2>"
                      + "<p class=\"precio\">" + Plantilla.Pesos(producto.Precio) + "</p>"
                      + "<div class=\"producto-acciones\">" + boton + marca + "</div>"
                      + "</div>"
                      + "</article>";
        }

        var marcadores = new Dictionary<string, string>
        {
            ["AVISO"] = Plantilla.Aviso(aviso),
            ["TARJETAS"] = tarjetas,
            ["BARRA_PEDIDO"] = BarraPedido(pedido)
        };
        return Plantilla.Pagina("Menú del día", "alumno", "catalogo.html", marcadores);
    }

    // Barra de abajo: "Ver pedido (N) · $TOTAL"
    private static string BarraPedido(PedidoNuevo pedido)
    {
        int cantidad = pedido.CantidadTotal();
        if (cantidad == 0)
        {
            return "<p class=\"barra-vacia\">Tocá \"Agregar\" para armar tu pedido.</p>";
        }
        return "<a href=\"/pedido\" class=\"barra-pedido\">Ver pedido (" + cantidad + ") · "
             + Plantilla.Pesos(pedido.Total()) + "</a>";
    }

    // Suma 1 unidad del producto elegido y vuelve al catálogo
    private static async Task<IResult> AgregarProducto(HttpContext contexto)
    {
        var formulario = await contexto.Request.ReadFormAsync();
        int.TryParse(formulario["id"], out int id);

        Producto? producto = PedidosRepositorio.ObtenerProducto(id);
        if (producto == null)
        {
            return Results.Redirect("/");
        }

        PedidoNuevo pedido = LeerPedidoEnCurso(contexto);
        ItemPedido? item = BuscarItem(pedido, producto.Nombre);
        int cantidadActual = 0;
        if (item != null) cantidadActual = item.Cantidad;

        if (cantidadActual >= 20)
        {
            return Results.Redirect("/?aviso=maximo");
        }
        if (cantidadActual + 1 > producto.Stock)
        {
            return Results.Redirect("/?aviso=stock");
        }

        if (item == null)
        {
            pedido.Productos!.Add(new ItemPedido
            {
                Nombre = producto.Nombre,
                Cantidad = 1,
                Precio = producto.Precio
            });
        }
        else
        {
            item.Cantidad = item.Cantidad + 1;
            item.Precio = producto.Precio;
        }

        GuardarPedidoEnCurso(contexto, pedido);
        // "#producto-1" hace que el navegador vuelva a mostrar la misma tarjeta
        return Results.Redirect("/#producto-" + producto.Id);
    }

    // ================= Paso 2: mi pedido y recreo =================

    private static IResult MostrarPedido(HttpContext contexto)
    {
        PedidoNuevo pedido = LeerPedidoEnCurso(contexto);
        if (pedido.CantidadTotal() == 0)
        {
            return Results.Redirect("/?aviso=vacio");
        }

        string aviso = "";
        string codigoAviso = contexto.Request.Query["aviso"].ToString();
        if (codigoAviso == "recreo") aviso = "Elegí en qué recreo lo retirás: 10:10 o 12:15.";

        // Un renglón por producto, con el botón para quitar una unidad
        string items = "";
        foreach (var item in pedido.Productos!)
        {
            string nombre = Plantilla.Texto(item.Nombre);
            items += "<li class=\"item\">"
                   + "<span class=\"item-nombre\">" + item.Cantidad + " × " + nombre + "</span>"
                   + "<span class=\"item-precio\">" + Plantilla.Pesos(item.Subtotal()) + "</span>"
                   + "<form method=\"post\" action=\"/pedido/quitar\">"
                   + "<input type=\"hidden\" name=\"nombre\" value=\"" + nombre + "\">"
                   + "<button type=\"submit\" class=\"boton-quitar\" aria-label=\"Quitar un " + nombre + "\">Quitar</button>"
                   + "</form>"
                   + "</li>";
        }

        // Si ya había elegido un recreo, lo dejamos marcado
        string marca1 = "";
        string marca2 = "";
        if (pedido.Recreo == PedidoNuevo.Recreo1) marca1 = "checked";
        if (pedido.Recreo == PedidoNuevo.Recreo2) marca2 = "checked";

        var marcadores = new Dictionary<string, string>
        {
            ["AVISO"] = Plantilla.Aviso(aviso),
            ["ITEMS"] = items,
            ["TOTAL"] = Plantilla.Pesos(pedido.Total()),
            ["MARCA_1010"] = marca1,
            ["MARCA_1215"] = marca2
        };
        return Plantilla.Pagina("Mi pedido", "alumno", "pedido.html", marcadores);
    }

    // Quita una unidad del producto. Si llega a 0, lo saca del pedido.
    private static async Task<IResult> QuitarProducto(HttpContext contexto)
    {
        var formulario = await contexto.Request.ReadFormAsync();
        string nombre = formulario["nombre"].ToString();

        PedidoNuevo pedido = LeerPedidoEnCurso(contexto);
        ItemPedido? item = BuscarItem(pedido, nombre);
        if (item != null)
        {
            item.Cantidad = item.Cantidad - 1;
            if (item.Cantidad <= 0)
            {
                pedido.Productos!.Remove(item);
            }
            GuardarPedidoEnCurso(contexto, pedido);
        }

        if (pedido.CantidadTotal() == 0)
        {
            return Results.Redirect("/?aviso=vacio");
        }
        return Results.Redirect("/pedido");
    }

    // Guarda el recreo elegido y pasa a "Tus datos".
    // El navegador ya obliga a elegir (required), pero lo revisamos también acá.
    private static async Task<IResult> ElegirRecreo(HttpContext contexto)
    {
        var formulario = await contexto.Request.ReadFormAsync();
        string recreo = formulario["recreo"].ToString();

        PedidoNuevo pedido = LeerPedidoEnCurso(contexto);
        if (pedido.CantidadTotal() == 0)
        {
            return Results.Redirect("/?aviso=vacio");
        }
        if (!PedidoNuevo.RecreoValido(recreo))
        {
            return Results.Redirect("/pedido?aviso=recreo");
        }

        pedido.Recreo = recreo;
        GuardarPedidoEnCurso(contexto, pedido);
        return Results.Redirect("/datos");
    }

    // ================= Ayudas para la sesión =================

    // Lee el pedido en curso de la sesión (o uno vacío si no hay)
    private static PedidoNuevo LeerPedidoEnCurso(HttpContext contexto)
    {
        string? json = contexto.Session.GetString("pedido");
        if (string.IsNullOrEmpty(json))
        {
            return new PedidoNuevo();
        }
        PedidoNuevo? pedido = JsonSerializer.Deserialize<PedidoNuevo>(json);
        if (pedido == null) return new PedidoNuevo();
        if (pedido.Productos == null) pedido.Productos = new List<ItemPedido>();
        return pedido;
    }

    // Guarda el pedido en curso en la sesión, como texto JSON
    private static void GuardarPedidoEnCurso(HttpContext contexto, PedidoNuevo pedido)
    {
        contexto.Session.SetString("pedido", JsonSerializer.Serialize(pedido));
    }

    private static ItemPedido? BuscarItem(PedidoNuevo pedido, string nombre)
    {
        foreach (var item in pedido.Productos!)
        {
            if (item.Nombre == nombre) return item;
        }
        return null;
    }

    private static int CantidadEnPedido(PedidoNuevo pedido, string nombre)
    {
        ItemPedido? item = BuscarItem(pedido, nombre);
        if (item == null) return 0;
        return item.Cantidad;
    }
}

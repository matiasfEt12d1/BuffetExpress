using System.Text.Json.Serialization;

namespace BuffetExpress.Models;

// Pedido que todavía no se guardó en la base.
// Lo usa la API (llega en el JSON) y también la vista del alumno
// (se guarda en la sesión mientras el alumno va pasando de pantalla).
public class PedidoNuevo
{
    public const string Recreo1 = "10:10";
    public const string Recreo2 = "12:15";

    [JsonPropertyName("nombre")]
    public string? Nombre { get; set; } = "";

    [JsonPropertyName("curso")]
    public string? Curso { get; set; } = "";

    [JsonPropertyName("recreo")]
    public string? Recreo { get; set; } = "";

    [JsonPropertyName("productos")]
    public List<ItemPedido>? Productos { get; set; } = new List<ItemPedido>();

    // Cantidad de unidades en total (para "Ver pedido (N)")
    public int CantidadTotal()
    {
        int cantidad = 0;
        if (Productos == null) return 0;
        foreach (var item in Productos)
        {
            cantidad += item.Cantidad;
        }
        return cantidad;
    }

    // Suma de los precios
    public int Total()
    {
        int total = 0;
        if (Productos == null) return 0;
        foreach (var item in Productos)
        {
            total += item.Subtotal();
        }
        return total;
    }

    // ¿El recreo es uno de los dos válidos?
    public static bool RecreoValido(string? recreo)
    {
        return recreo == Recreo1 || recreo == Recreo2;
    }

    // Revisa que no falte nada. Devuelve el mensaje de error,
    // o un texto vacío si está todo bien.
    public string Validar()
    {
        if (string.IsNullOrWhiteSpace(Nombre))
            return "Falta el nombre y apellido.";
        if (Nombre.Trim().Length > 100)
            return "El nombre y apellido puede tener hasta 100 letras.";
        if (string.IsNullOrWhiteSpace(Curso))
            return "Falta el curso y división.";
        if (Curso.Trim().Length > 20)
            return "El curso y división puede tener hasta 20 letras.";
        if (!RecreoValido(Recreo))
            return "Elegí en qué recreo lo retirás: 10:10 o 12:15.";
        if (Productos == null || Productos.Count == 0)
            return "El pedido no tiene productos.";

        foreach (var item in Productos)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Nombre))
                return "Falta el nombre de un producto.";
            if (item.Cantidad < 1 || item.Cantidad > 20)
                return "La cantidad de cada producto tiene que ser entre 1 y 20.";
        }
        return "";
    }
}

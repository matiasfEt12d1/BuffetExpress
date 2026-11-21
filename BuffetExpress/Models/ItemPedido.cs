using System.Text.Json.Serialization;

namespace BuffetExpress.Models;

// Un renglón del pedido: qué producto y cuántos
public class ItemPedido
{
    // Solo se usa adentro del servidor (no viaja en el JSON)
    [JsonIgnore]
    public int ProductoId { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = "";

    [JsonPropertyName("cantidad")]
    public int Cantidad { get; set; }

    // Precio de una unidad. En la API se ignora el que manda el cliente:
    // el servidor siempre usa el precio de la base de datos.
    [JsonPropertyName("precio")]
    public int Precio { get; set; }

    public int Subtotal()
    {
        return Cantidad * Precio;
    }
}

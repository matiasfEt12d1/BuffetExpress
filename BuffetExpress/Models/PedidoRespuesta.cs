using System.Text.Json.Serialization;

namespace BuffetExpress.Models;

// Lo que devuelve la API cuando el pedido se guardó bien (código 201)
public class PedidoRespuesta
{
    [JsonPropertyName("id_pedido")]
    public int IdPedido { get; set; }

    [JsonPropertyName("codigo_retiro")]
    public string CodigoRetiro { get; set; } = "";

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("recreo")]
    public string Recreo { get; set; } = "";

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = "";

    // Fecha como "24/11/2026"
    [JsonPropertyName("fecha")]
    public string Fecha { get; set; } = "";
}

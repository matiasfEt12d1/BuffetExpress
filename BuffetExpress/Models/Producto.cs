using System.Text.Json.Serialization;

namespace BuffetExpress.Models;

// Un producto del menú (tabla productos)
public class Producto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = "";

    [JsonPropertyName("precio")]
    public int Precio { get; set; }

    [JsonPropertyName("stock")]
    public int Stock { get; set; }

    // Nombre del dibujo SVG que está en wwwroot/img (no hay fotos)
    [JsonIgnore]
    public string Imagen
    {
        get
        {
            switch (Nombre)
            {
                case "Tostado": return "tostado.svg";
                case "Sándwich de milanesa": return "sandwich-milanesa.svg";
                case "Agua 500 ml": return "agua.svg";
                case "Alfajor": return "alfajor.svg";
                case "Jugo de naranja": return "jugo.svg";
                default: return "producto.svg";
            }
        }
    }
}

namespace BuffetExpress.Models;

// Un pedido ya guardado en la base (tabla pedidos + sus ítems)
public class Pedido
{
    // Estados posibles, en orden
    public const string Pendiente = "Pendiente";
    public const string Listo = "Listo para retirar";
    public const string Entregado = "Entregado";

    public int Id { get; set; }
    public string CodigoRetiro { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Curso { get; set; } = "";
    public string Recreo { get; set; } = "";
    public int Total { get; set; }
    public string Estado { get; set; } = Pendiente;
    public DateTime Fecha { get; set; }
    public List<ItemPedido> Items { get; set; } = new List<ItemPedido>();

    // Texto corto con los productos: "1 Tostado · 1 Agua 500 ml"
    public string ResumenItems()
    {
        var partes = new List<string>();
        foreach (var item in Items)
        {
            partes.Add(item.Cantidad + " " + item.Nombre);
        }
        return string.Join(" · ", partes);
    }
}

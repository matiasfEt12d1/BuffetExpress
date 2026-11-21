using System.Globalization;
using System.Net;

namespace BuffetExpress.Plantillas;

// Arma las páginas: lee un archivo .html de la carpeta Plantillas
// y reemplaza los marcadores como {{CODIGO}} por los valores reales.
public static class Plantilla
{
    private static string carpeta = "";

    // Se llama una sola vez al arrancar, desde Program.cs
    public static void Configurar(string carpetaPlantillas)
    {
        carpeta = carpetaPlantillas;
    }

    // Devuelve el texto de un archivo de la carpeta Plantillas
    public static string Leer(string nombreArchivo)
    {
        return File.ReadAllText(Path.Combine(carpeta, nombreArchivo));
    }

    // Arma una página completa:
    // 1) lee la plantilla de la pantalla y reemplaza sus marcadores
    // 2) la mete dentro de marco.html (logo arriba y pie abajo)
    // "estilo" es "alumno" o "buffet" y elige la hoja de estilos.
    public static IResult Pagina(string titulo, string estilo, string nombreArchivo,
        Dictionary<string, string> marcadores, int codigoHttp = 200, string cabeceraExtra = "")
    {
        string contenido = Leer(nombreArchivo);
        foreach (var marcador in marcadores)
        {
            contenido = contenido.Replace("{{" + marcador.Key + "}}", marcador.Value);
        }

        string html = Leer("marco.html")
            .Replace("{{TITULO}}", Texto(titulo))
            .Replace("{{ESTILO}}", estilo)
            .Replace("{{CABECERA_EXTRA}}", cabeceraExtra)
            .Replace("{{CONTENIDO}}", contenido);

        return Results.Content(html, "text/html; charset=utf-8", null, codigoHttp);
    }

    // Página de error simple (por ejemplo, un ticket que no existe)
    public static IResult Error(string mensaje, int codigoHttp)
    {
        var marcadores = new Dictionary<string, string>
        {
            ["MENSAJE"] = Texto(mensaje)
        };
        return Pagina("Ups", "alumno", "error.html", marcadores, codigoHttp);
    }

    // Escapa lo que escribió el usuario para que no se pueda meter HTML
    public static string Texto(string? texto)
    {
        return WebUtility.HtmlEncode(texto ?? "");
    }

    // Formatea un precio como en Argentina: 2500 -> "$2.500"
    public static string Pesos(int monto)
    {
        return "$" + monto.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", ".");
    }

    // Etiqueta tipo píldora para el estado del pedido
    public static string Etiqueta(string estado)
    {
        string clase = "etiqueta-pendiente";
        if (estado == "Listo para retirar") clase = "etiqueta-listo";
        if (estado == "Entregado") clase = "etiqueta-entregado";
        return "<span class=\"etiqueta " + clase + "\">" + Texto(estado) + "</span>";
    }

    // Cuadro de aviso (error o confirmación). Si no hay mensaje, no muestra nada.
    public static string Aviso(string mensaje, bool esError = true)
    {
        if (string.IsNullOrEmpty(mensaje)) return "";
        string clase = esError ? "aviso aviso-error" : "aviso aviso-ok";
        string rol = esError ? "alert" : "status";
        return "<p class=\"" + clase + "\" role=\"" + rol + "\">" + Texto(mensaje) + "</p>";
    }
}

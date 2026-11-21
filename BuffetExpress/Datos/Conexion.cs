using MySqlConnector;

namespace BuffetExpress.Datos;

// Abre las conexiones a MySQL y crea la base si todavía no existe.
public static class Conexion
{
    private static string cadenaConexion = "";

    // Se llama una sola vez al arrancar, desde Program.cs
    public static void Configurar(string cadena)
    {
        cadenaConexion = cadena;
    }

    // Devuelve una conexión ya abierta. Hay que cerrarla con "using".
    public static MySqlConnection Abrir()
    {
        var conexion = new MySqlConnection(cadenaConexion);
        conexion.Open();
        return conexion;
    }

    // Si la base de datos no existe, ejecuta database/script.sql para crearla.
    // Así no hace falta instalar el cliente "mysql" ni en Windows ni en Linux.
    public static void CrearBaseSiNoExiste(string rutaScript)
    {
        // Nos conectamos al servidor sin elegir base, porque puede no existir
        var datos = new MySqlConnectionStringBuilder(cadenaConexion);
        string nombreBase = datos.Database;
        datos.Database = "";

        using var conexion = new MySqlConnection(datos.ConnectionString);
        conexion.Open();

        using var consulta = new MySqlCommand(
            "SELECT COUNT(*) FROM information_schema.SCHEMATA WHERE SCHEMA_NAME = @nombre", conexion);
        consulta.Parameters.AddWithValue("@nombre", nombreBase);
        long cantidad = Convert.ToInt64(consulta.ExecuteScalar());

        if (cantidad > 0)
        {
            Console.WriteLine("Base de datos '" + nombreBase + "' encontrada.");
            return;
        }

        Console.WriteLine("La base '" + nombreBase + "' no existe. Ejecutando " + rutaScript + " ...");
        string script = File.ReadAllText(rutaScript);
        using var crear = new MySqlCommand(script, conexion);
        crear.ExecuteNonQuery();
        Console.WriteLine("Base de datos creada con el menú inicial.");
    }
}

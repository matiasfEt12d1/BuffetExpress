using System.Text.Encodings.Web;
using System.Text.Unicode;
using BuffetExpress.Datos;
using BuffetExpress.Plantillas;
using BuffetExpress.Rutas;

var builder = WebApplication.CreateBuilder(args);

// Configuración local con la contraseña de MySQL (no se sube a GitHub).
builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true);
// La variable de entorno ConnectionStrings__BuffetExpress tiene prioridad (útil en Linux).
builder.Configuration.AddEnvironmentVariables();

// Que el JSON muestre los acentos tal cual ("Pérez" y no "Pérez")
builder.Services.ConfigureHttpJsonOptions(opciones =>
{
    opciones.SerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
});

// Sesión del servidor: guarda el pedido en curso mientras el alumno elige
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opciones =>
{
    opciones.Cookie.Name = "BuffetExpress.Sesion";
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.IsEssential = true;
    opciones.IdleTimeout = TimeSpan.FromMinutes(30);
});

var app = builder.Build();

// ---------- Base de datos ----------
string? cadena = builder.Configuration.GetConnectionString("BuffetExpress");
if (string.IsNullOrWhiteSpace(cadena))
{
    Console.WriteLine("Falta la cadena de conexión 'BuffetExpress'.");
    Console.WriteLine("Copiá appsettings.ejemplo.json como appsettings.Development.json y poné tu contraseña,");
    Console.WriteLine("o definí la variable de entorno ConnectionStrings__BuffetExpress.");
    return;
}
Conexion.Configurar(cadena);

try
{
    // Si la base no existe, la crea con database/script.sql
    string rutaScript = Path.Combine(app.Environment.ContentRootPath, "..", "database", "script.sql");
    Conexion.CrearBaseSiNoExiste(rutaScript);
}
catch (Exception ex)
{
    Console.WriteLine("No se pudo conectar a MySQL: " + ex.Message);
    Console.WriteLine("Revisá que MySQL esté prendido y que la contraseña sea la correcta.");
    return;
}

// Carpeta donde están las plantillas HTML de las páginas
Plantilla.Configurar(Path.Combine(app.Environment.ContentRootPath, "Plantillas"));

// Si algo falla, mostramos una página de error en vez de una pantalla en blanco
app.UseExceptionHandler("/error");
app.Map("/error", () => Plantilla.Error("Hubo un problema en el servidor. Probá de nuevo en un ratito.", 500));

// Sirve los archivos de wwwroot (CSS e imágenes)
app.UseStaticFiles();
app.UseSession();

// ---------- Rutas ----------
RutasApi.Registrar(app);
RutasAlumno.Registrar(app);

app.Run();

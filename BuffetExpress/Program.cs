using BuffetExpress.Plantillas;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Carpeta donde están las plantillas HTML de las páginas
Plantilla.Configurar(Path.Combine(app.Environment.ContentRootPath, "Plantillas"));

// Sirve los archivos de wwwroot (CSS e imágenes)
app.UseStaticFiles();

app.Run();

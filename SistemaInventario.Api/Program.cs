using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using SistemaInventario.Api.Infrastructure.Database;
using SistemaInventario.Api.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Ensure images folder exists (from configuration)
var imagesRelativePath = builder.Configuration.GetValue<string>("FileStorage:ImagesPath")?.Trim() ?? "wwwroot/images/";
var imagesAbsolutePath = Path.GetFullPath(imagesRelativePath, builder.Environment.ContentRootPath);
Directory.CreateDirectory(imagesAbsolutePath);

// Registrar los handlers en el contenedor de dependencias
builder.Services.AddScoped<SistemaInventario.Api.Features.Auth.RegistroHandler>();
builder.Services.AddScoped<SistemaInventario.Api.Features.Auth.LoginHandler>();

// Database (in-memory for Phase 1)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("InventarioDbMock"));

// Security
builder.Services.AddSingleton<IJwtProvider, JwtProvider>();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Keep any existing AddOpenApi extension if present
try
{
    builder.Services.AddOpenApi();
}
catch { /* ignore if AddOpenApi not available */ }

var app = builder.Build();

// Enable Swagger UI for all environments so the health endpoint is discoverable
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sistema Inventario API V1"));

// Map legacy openapi if available
try
{
    app.MapOpenApi();
}
catch { /* ignore if MapOpenApi not available */ }

app.UseHttpsRedirection();
app.UseStaticFiles();

// Health endpoint (API check)
app.MapGet("/api/health", (IConfiguration config, IWebHostEnvironment env) =>
{
    var result = new
    {
        status = "Running",
        environment = env.EnvironmentName,
        imagesPath = config["FileStorage:ImagesPath"],
        timestamp = DateTime.UtcNow
    };
    return Results.Ok(result);
})
.WithName("ApiHealth")
.WithOpenApi();

// Mapear las rutas de autenticación
SistemaInventario.Api.Features.Auth.RegistroEndpoint.Map(app);
SistemaInventario.Api.Features.Auth.LoginEndpoint.Map(app);

app.Run();

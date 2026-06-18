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

builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "Pega tu token JWT aquí.\n\nNota: No es necesario escribir 'Bearer ' al inicio."
    });

    options.AddSecurityRequirement(document => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });

    // ACTIVA EL FILTRO PARA QUITAR CANDADOS A LAS EXCEPCIONES
    options.OperationFilter<SistemaInventario.Api.Infrastructure.Security.QuitarCandadoFiltro>();
});
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
.AllowAnonymous()
.WithName("ApiHealth")
.WithOpenApi();

// Mapear las rutas de autenticación
SistemaInventario.Api.Features.Auth.RegistroEndpoint.Map(app);
SistemaInventario.Api.Features.Auth.LoginEndpoint.Map(app);

// Mapear rutas de Revisiones
SistemaInventario.Api.Features.Revisiones.CrearRevision.Map(app);
SistemaInventario.Api.Features.Revisiones.GetRevisiones.Map(app);
SistemaInventario.Api.Features.Revisiones.GetRevisionById.Map(app);
SistemaInventario.Api.Features.Revisiones.EscanearCodigo.Map(app);
SistemaInventario.Api.Features.Revisiones.FinalizarRevision.Map(app);

app.Run();


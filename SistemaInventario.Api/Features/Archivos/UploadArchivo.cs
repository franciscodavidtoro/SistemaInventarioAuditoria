using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using SistemaInventario.Api.Domain.Entities;
using SistemaInventario.Api.Infrastructure.Database;
using SistemaInventario.Api.Infrastructure.Storage;

namespace SistemaInventario.Api.Features.Archivos;

// --- DTOs (Request / Response) ---
public class UploadArchivoRequest
{
    public IFormFile Archivo { get; set; } = default!;
}

public class UploadArchivoResponse
{
    public Guid Id { get; set; }
    public string NombreOriginal { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string TipoMime { get; set; } = string.Empty;
    public long TamanioBytes { get; set; }
    public string RutaRelativa { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}

// --- Endpoint / Controlador ---
public static class UploadArchivoEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/archivos", async (HttpRequest request, UploadArchivoHandler handler, HttpContext http) => await handler.HandleAsync(request, http))
            .RequireAuthorization()
            .WithTags("Archivos")
            .WithSummary("Subir un nuevo archivo")
            .WithDescription("Recibe un archivo mediante multipart/form-data, lo almacena físicamente con un nombre único (UUID) y registra su información en la base de datos.")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<UploadArchivoResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}

// --- Lógica de Negocio (Handler) ---
public class UploadArchivoHandler
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _storage;

    public UploadArchivoHandler(ApplicationDbContext db, IFileStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<IResult> HandleAsync(HttpRequest request, HttpContext http)
    {
        if (!request.HasFormContentType)
            return Results.BadRequest("El contenido debe ser multipart/form-data.");

        var form = await request.ReadFormAsync();
        var archivo = form.Files.GetFile("archivo");
        if (archivo == null)
            return Results.BadRequest("Se requiere un archivo con el nombre 'archivo'.");

        if (archivo.Length <= 0)
            return Results.BadRequest("El archivo está vacío.");

        if (archivo.Length > _storage.TamanioMaximoBytes)
            return Results.BadRequest($"El archivo supera el tamaño máximo permitido de {_storage.TamanioMaximoBytes / (1024 * 1024)} MB.");

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!_storage.EsExtensionValida(extension))
            return Results.BadRequest("La extensión del archivo no está permitida.");

        if (!_storage.EsMimeTypeValido(archivo.ContentType))
            return Results.BadRequest("El tipo de contenido (MIME) del archivo no está permitido.");

        var usuarioId = GetLoggedUserId(http);
        if (usuarioId == null)
            return Results.Forbid();

        (string NombreFisico, string Extension, string RutaRelativa) guardado;
        try
        {
            guardado = await _storage.GuardarAsync(archivo);
        }
        catch (IOException)
        {
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }

        var entidad = new Archivo
        {
            Id = Guid.NewGuid(),
            NombreOriginal = archivo.FileName,
            NombreFisico = guardado.NombreFisico,
            Extension = guardado.Extension,
            TipoMime = archivo.ContentType,
            TamanioBytes = archivo.Length,
            RutaRelativa = guardado.RutaRelativa,
            FechaCreacion = DateTime.UtcNow,
            FechaModificacion = null,
            UsuarioIdPropietario = usuarioId.Value
        };

        try
        {
            await _db.Archivos.AddAsync(entidad);
            await _db.SaveChangesAsync();
        }
        catch (Exception)
        {
            _storage.Eliminar(guardado.RutaRelativa);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }

        return Results.Created($"/api/archivos/{entidad.Id}", new UploadArchivoResponse
        {
            Id = entidad.Id,
            NombreOriginal = entidad.NombreOriginal,
            Extension = entidad.Extension,
            TipoMime = entidad.TipoMime,
            TamanioBytes = entidad.TamanioBytes,
            RutaRelativa = entidad.RutaRelativa,
            FechaCreacion = entidad.FechaCreacion
        });
    }

    private Guid? GetLoggedUserId(HttpContext http)
    {
        var claim = http.User.FindFirst(ClaimTypes.NameIdentifier) ?? http.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (claim == null || !Guid.TryParse(claim.Value, out var usuarioId))
            return null;
        return usuarioId;
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SistemaInventario.Api.Infrastructure.Database;
using SistemaInventario.Api.Infrastructure.Storage;

namespace SistemaInventario.Api.Features.Archivos;

// --- DTOs (Request / Response) ---
public class UpdateArchivoRequest
{
    public IFormFile Archivo { get; set; } = default!;
}

public class UpdateArchivoResponse
{
    public Guid Id { get; set; }
    public string NombreOriginal { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string TipoMime { get; set; } = string.Empty;
    public long TamanioBytes { get; set; }
    public string RutaRelativa { get; set; } = string.Empty;
    public DateTime FechaModificacion { get; set; }
}

// --- Endpoint / Controlador ---
public static class UpdateArchivoEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/archivos/{id}", async (string id, HttpRequest request, UpdateArchivoHandler handler, HttpContext http) => await handler.HandleAsync(id, request, http))
            .RequireAuthorization()
            .WithTags("Archivos")
            .WithSummary("Reemplazar un archivo existente")
            .WithDescription("Sube un nuevo archivo, actualiza el registro en la base de datos y elimina el archivo físico anterior si el usuario es propietario o administrador.")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<UpdateArchivoResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}

// --- Lógica de Negocio (Handler) ---
public class UpdateArchivoHandler
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _storage;

    public UpdateArchivoHandler(ApplicationDbContext db, IFileStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<IResult> HandleAsync(string id, HttpRequest request, HttpContext http)
    {
        if (!Guid.TryParse(id, out var archivoId))
            return Results.BadRequest("Id inválido.");

        var entidad = await _db.Archivos.FirstOrDefaultAsync(a => a.Id == archivoId);
        if (entidad == null)
            return Results.NotFound(new { message = $"Archivo con ID {archivoId} no encontrado." });

        var usuarioId = GetLoggedUserId(http);
        if (usuarioId == null)
            return Results.Forbid();

        var rol = GetLoggedUserRole(http);
        if (entidad.UsuarioIdPropietario != usuarioId.Value && !string.Equals(rol, "Admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

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

        (string NombreFisico, string Extension, string RutaRelativa) guardado;
        try
        {
            guardado = await _storage.GuardarAsync(archivo);
        }
        catch (IOException)
        {
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }

        var rutaAnterior = entidad.RutaRelativa;

        entidad.NombreOriginal = archivo.FileName;
        entidad.NombreFisico = guardado.NombreFisico;
        entidad.Extension = guardado.Extension;
        entidad.TipoMime = archivo.ContentType;
        entidad.TamanioBytes = archivo.Length;
        entidad.RutaRelativa = guardado.RutaRelativa;
        entidad.FechaModificacion = DateTime.UtcNow;

        try
        {
            _db.Archivos.Update(entidad);
            await _db.SaveChangesAsync();
        }
        catch (Exception)
        {
            // La base de datos no se actualizó: se elimina el archivo nuevo para no dejarlo huérfano
            // y se conserva el archivo anterior, manteniendo la consistencia.
            _storage.Eliminar(guardado.RutaRelativa);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }

        // Solo se elimina el archivo físico anterior una vez confirmado el éxito en la base de datos.
        _storage.Eliminar(rutaAnterior);

        return Results.Ok(new UpdateArchivoResponse
        {
            Id = entidad.Id,
            NombreOriginal = entidad.NombreOriginal,
            Extension = entidad.Extension,
            TipoMime = entidad.TipoMime,
            TamanioBytes = entidad.TamanioBytes,
            RutaRelativa = entidad.RutaRelativa,
            FechaModificacion = entidad.FechaModificacion!.Value
        });
    }

    private Guid? GetLoggedUserId(HttpContext http)
    {
        var claim = http.User.FindFirst(ClaimTypes.NameIdentifier) ?? http.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (claim == null || !Guid.TryParse(claim.Value, out var usuarioId))
            return null;
        return usuarioId;
    }

    private string GetLoggedUserRole(HttpContext http)
    {
        return http.User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }
}

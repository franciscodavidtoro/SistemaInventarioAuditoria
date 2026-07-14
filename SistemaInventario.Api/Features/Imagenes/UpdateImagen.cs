using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SistemaInventario.Api.Infrastructure.Database;

namespace SistemaInventario.Api.Features.Imagenes;

// --- DTOs (Request / Response) ---
public class UpdateImagenResponse
{
    public string Id { get; set; } = string.Empty;
}

// --- Endpoint / Controlador ---
public static class UpdateImagenEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/imagenes/{id}", async (string id, HttpRequest request, UpdateImagenHandler handler, HttpContext http) => await handler.HandleAsync(id, request, http))
            .RequireAuthorization()
            .WithTags("Imagenes")
            .WithSummary("Reemplazar el archivo de una imagen existente")
            .WithDescription("Sustituye el archivo físico de una imagen ya registrada, conservando su Id y su asociación con la entidad.")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<UpdateImagenResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }
}

// --- Lógica de Negocio (Handler) ---
public class UpdateImagenHandler
{
    private readonly ApplicationDbContext _db;
    private readonly ImagenStorage _storage;

    public UpdateImagenHandler(ApplicationDbContext db, ImagenStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<IResult> HandleAsync(string id, HttpRequest request, HttpContext http)
    {
        if (!Guid.TryParse(id, out var imagenId))
            return Results.BadRequest("Id inválido.");

        var imagen = await _db.Imagenes.FirstOrDefaultAsync(i => i.Id == imagenId);
        if (imagen == null)
            return Results.NotFound();

        var userId = ImagenReglas.GetLoggedUserId(http);
        if (userId == null)
            return Results.Forbid();

        var (existe, propietarioId) = await ImagenReglas.ValidarEntidadAsync(_db, imagen.EntidadTipo, imagen.EntidadId);
        var rol = ImagenReglas.GetLoggedUserRole(http);
        if (existe && !ImagenReglas.PuedeGestionar(propietarioId, userId.Value, rol))
            return Results.Forbid();

        if (!request.HasFormContentType)
            return Results.BadRequest("El contenido debe ser multipart/form-data.");

        var form = await request.ReadFormAsync();
        var archivo = form.Files.GetFile("archivo");
        if (archivo == null)
            return Results.BadRequest("Se requiere un archivo con el nombre 'archivo'.");

        if (archivo.Length <= 0 || archivo.Length > ImagenReglas.MaxFileSizeBytes)
            return Results.BadRequest("El archivo es demasiado grande o está vacío.");

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!ImagenReglas.ExtensionesPermitidas.Contains(extension) || !archivo.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest("El archivo debe ser una imagen (jpg, jpeg, png, gif o webp).");

        var nombreAnterior = imagen.NombreArchivo;
        var nombreNuevo = await _storage.GuardarAsync(archivo);

        imagen.NombreArchivo = nombreNuevo;
        imagen.ContentType = archivo.ContentType;

        _db.Imagenes.Update(imagen);
        await _db.SaveChangesAsync();

        _storage.Eliminar(nombreAnterior);

        return Results.Ok(new UpdateImagenResponse { Id = imagen.Id.ToString() });
    }
}

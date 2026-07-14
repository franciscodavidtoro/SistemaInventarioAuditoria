using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SistemaInventario.Api.Domain.Entities;
using SistemaInventario.Api.Infrastructure.Database;

namespace SistemaInventario.Api.Features.Imagenes;

// --- DTOs (Request / Response) ---
// El único dato que aporta quien sube el archivo es el propio archivo:
// la entidad dueña (tipo + id) se deduce de la ruta llamada.
public class SubirImagenRequest
{
    public IFormFile Archivo { get; set; } = default!;
}

public class CreateImagenResponse
{
    public string Id { get; set; } = string.Empty;
}

// --- Endpoint / Controlador ---
// Se registra una ruta de subida anidada por cada tipo de entidad soportado,
// para que el EntidadId salga de la URL (el mismo id que el servidor ya
// entregó al crear esa entidad) y el EntidadTipo quede fijo según la ruta.
public static class CreateImagenEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/elementos/{elementoId:guid}/imagenes",
            async (Guid elementoId, [FromForm] SubirImagenRequest request, CreateImagenHandler handler, HttpContext http)
                => await handler.HandleAsync("Elemento", elementoId, request.Archivo, http))
            .DisableAntiforgery()
            .RequireAuthorization()
            .WithTags("Imagenes")
            .WithSummary("Subir una imagen para un elemento")
            .WithDescription("Almacena el archivo en el servidor con un nombre único (UUID) y lo asocia al elemento indicado en la URL.")
            .Produces<CreateImagenResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/usuarios/{usuarioId:guid}/imagenes",
            async (Guid usuarioId, [FromForm] SubirImagenRequest request, CreateImagenHandler handler, HttpContext http)
                => await handler.HandleAsync("Usuario", usuarioId, request.Archivo, http))
            .DisableAntiforgery()
            .RequireAuthorization()
            .WithTags("Imagenes")
            .WithSummary("Subir una imagen de perfil para un usuario")
            .WithDescription("Almacena el archivo en el servidor con un nombre único (UUID) y lo asocia al usuario indicado en la URL.")
            .Produces<CreateImagenResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }
}

// --- Lógica de Negocio (Handler) ---
public class CreateImagenHandler
{
    private readonly ApplicationDbContext _db;
    private readonly ImagenStorage _storage;

    public CreateImagenHandler(ApplicationDbContext db, ImagenStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<IResult> HandleAsync(string entidadTipo, Guid entidadId, IFormFile archivo, HttpContext http)
    {
        if (archivo == null)
            return Results.BadRequest("Se requiere un archivo con el nombre 'Archivo'.");

        if (archivo.Length <= 0 || archivo.Length > ImagenReglas.MaxFileSizeBytes)
            return Results.BadRequest("El archivo es demasiado grande o está vacío.");

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!ImagenReglas.ExtensionesPermitidas.Contains(extension) || !archivo.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest("El archivo debe ser una imagen (jpg, jpeg, png, gif o webp).");

        var userId = ImagenReglas.GetLoggedUserId(http);
        if (userId == null)
            return Results.Forbid();

        var (existe, propietarioId) = await ImagenReglas.ValidarEntidadAsync(_db, entidadTipo, entidadId);
        if (!existe)
            return Results.NotFound($"No existe {entidadTipo} con ese id.");

        var rol = ImagenReglas.GetLoggedUserRole(http);
        if (!ImagenReglas.PuedeGestionar(propietarioId, userId.Value, rol))
            return Results.Forbid();

        var nombreArchivo = await _storage.GuardarAsync(archivo);

        var imagen = new Imagen
        {
            Id = Guid.NewGuid(),
            NombreArchivo = nombreArchivo,
            ContentType = archivo.ContentType,
            EntidadTipo = entidadTipo,
            EntidadId = entidadId,
            UsuarioIdCarga = userId.Value,
            FechaCreacion = DateTime.UtcNow
        };

        await _db.Imagenes.AddAsync(imagen);
        await _db.SaveChangesAsync();

        return Results.Created($"/api/imagenes/{imagen.Id}", new CreateImagenResponse { Id = imagen.Id.ToString() });
    }
}

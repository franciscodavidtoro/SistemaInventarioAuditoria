using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SistemaInventario.Api.Domain.Entities;
using SistemaInventario.Api.Infrastructure.Database;

namespace SistemaInventario.Api.Features.Imagenes;

// --- DTOs (Request / Response) ---
public class CreateImagenRequest
{
    public string EntidadTipo { get; set; } = string.Empty;
    public Guid EntidadId { get; set; }
    public IFormFile Archivo { get; set; } = default!;
}

public class CreateImagenResponse
{
    public string Id { get; set; } = string.Empty;
}

// --- Endpoint / Controlador ---
public static class CreateImagenEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/imagenes", async ([FromForm] CreateImagenRequest request, CreateImagenHandler handler, HttpContext http) => await handler.HandleAsync(request, http))
            .DisableAntiforgery()
            .RequireAuthorization()
            .WithTags("Imagenes")
            .WithSummary("Subir una imagen y asociarla a una entidad")
            .WithDescription("Almacena el archivo en el servidor con un nombre único (UUID) y lo asocia a la entidad indicada (EntidadTipo/EntidadId).")
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

    public async Task<IResult> HandleAsync(CreateImagenRequest request, HttpContext http)
    {
        var archivo = request.Archivo;
        var entidadTipo = request.EntidadTipo;
        var entidadId = request.EntidadId;

        if (archivo == null)
            return Results.BadRequest("Se requiere un archivo con el nombre 'Archivo'.");

        if (archivo.Length <= 0 || archivo.Length > ImagenReglas.MaxFileSizeBytes)
            return Results.BadRequest("El archivo es demasiado grande o está vacío.");

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!ImagenReglas.ExtensionesPermitidas.Contains(extension) || !archivo.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest("El archivo debe ser una imagen (jpg, jpeg, png, gif o webp).");

        if (string.IsNullOrWhiteSpace(entidadTipo) || !ImagenReglas.TipoEntidadValido(entidadTipo))
            return Results.BadRequest("EntidadTipo inválido. Valores permitidos: " + string.Join(", ", ImagenReglas.TiposEntidadPermitidos));

        if (entidadId == Guid.Empty)
            return Results.BadRequest("EntidadId inválido.");

        var userId = ImagenReglas.GetLoggedUserId(http);
        if (userId == null)
            return Results.Forbid();

        var (existe, propietarioId) = await ImagenReglas.ValidarEntidadAsync(_db, entidadTipo, entidadId);
        if (!existe)
            return Results.NotFound("La entidad indicada no existe.");

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

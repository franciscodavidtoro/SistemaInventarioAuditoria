using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SistemaInventario.Api.Infrastructure.Database;

namespace SistemaInventario.Api.Features.Archivos;

// --- DTOs (Request / Response) ---
public class GetArchivoByIdRequest { }

public class GetArchivoByIdResponse
{
    public Guid Id { get; set; }
    public string NombreOriginal { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string TipoMime { get; set; } = string.Empty;
    public long TamanioBytes { get; set; }
    public string RutaRelativa { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public Guid UsuarioIdPropietario { get; set; }
}

// --- Endpoint / Controlador ---
public static class GetArchivoByIdEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/archivos/{id}", (string id, GetArchivoByIdHandler handler) => handler.HandleAsync(id))
            .RequireAuthorization()
            .WithTags("Archivos")
            .WithSummary("Obtener un archivo por su identificador")
            .WithDescription("Recupera la información registrada de un archivo específico. No existe endpoint de listado.")
            .Produces<GetArchivoByIdResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }
}

// --- Lógica de Negocio (Handler) ---
public class GetArchivoByIdHandler
{
    private readonly ApplicationDbContext _db;

    public GetArchivoByIdHandler(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IResult> HandleAsync(string id)
    {
        if (!Guid.TryParse(id, out var archivoId))
            return Results.BadRequest("Id inválido.");

        var archivo = await _db.Archivos
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == archivoId);

        if (archivo == null)
            return Results.NotFound(new { message = $"Archivo con ID {archivoId} no encontrado." });

        return Results.Ok(new GetArchivoByIdResponse
        {
            Id = archivo.Id,
            NombreOriginal = archivo.NombreOriginal,
            Extension = archivo.Extension,
            TipoMime = archivo.TipoMime,
            TamanioBytes = archivo.TamanioBytes,
            RutaRelativa = archivo.RutaRelativa,
            FechaCreacion = archivo.FechaCreacion,
            FechaModificacion = archivo.FechaModificacion,
            UsuarioIdPropietario = archivo.UsuarioIdPropietario
        });
    }
}

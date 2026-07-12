using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SistemaInventario.Api.Infrastructure.Database;
using SistemaInventario.Api.Infrastructure.Storage;

namespace SistemaInventario.Api.Features.Archivos;

// --- DTOs (Request / Response) ---
public class DeleteArchivoRequest { }
public class DeleteArchivoResponse { }

// --- Endpoint / Controlador ---
public static class DeleteArchivoEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/archivos/{id}", async (string id, DeleteArchivoHandler handler, HttpContext http) => await handler.HandleAsync(id, http))
            .RequireAuthorization()
            .WithTags("Archivos")
            .WithSummary("Eliminar un archivo")
            .WithDescription("Elimina primero el archivo físico del disco y luego su registro en la base de datos, si el usuario es propietario o administrador.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}

// --- Lógica de Negocio (Handler) ---
public class DeleteArchivoHandler
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _storage;

    public DeleteArchivoHandler(ApplicationDbContext db, IFileStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<IResult> HandleAsync(string id, HttpContext http)
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

        try
        {
            // Si el archivo físico ya no existe, no se considera un error fatal: se continúa
            // para eliminar el registro y evitar dejar basura en la base de datos.
            _storage.Eliminar(entidad.RutaRelativa);
        }
        catch (IOException)
        {
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }

        _db.Archivos.Remove(entidad);
        await _db.SaveChangesAsync();

        return Results.NoContent();
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

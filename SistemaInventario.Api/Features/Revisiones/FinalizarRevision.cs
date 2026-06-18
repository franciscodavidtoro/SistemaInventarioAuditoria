using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SistemaInventario.Api.Features.Revisiones;

public static class FinalizarRevision
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/revisiones/{id}/finalizar", HandleAsync)
            .RequireAuthorization()
            .WithTags("Procesos de Revisión y Auditoría")
            .WithSummary("Clausurar definitivamente una sesión de revisión activa")
            .WithDescription("Calcula la cobertura física y bloquea la sesión.")
            .Produces<Response>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden);
    }

    public record Response(string Id, string Estado, int ElementosFaltantes);

    private static IResult HandleAsync(string id)
    {
        // TODO: Lógica futura
        // 1. Extraer JWT UUID y validar propiedad o rol Admin (sino 403)
        // 2. Desencriptar 'id' a Guid
        // 3. Validar RowVersion para concurrencia optimista
        // 4. Comparar conteo de escaneos vs total de inventario
        // 5. Cambiar estado a Completada o Incompleta, poner FechaFin
        
        return Results.Ok(new Response(id, "Completada", 0));
    }
}
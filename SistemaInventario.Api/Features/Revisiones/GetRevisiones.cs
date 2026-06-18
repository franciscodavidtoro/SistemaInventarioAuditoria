using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SistemaInventario.Api.Features.Revisiones;

public static class GetRevisiones
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/revisiones", HandleAsync)
            .RequireAuthorization()
            .WithTags("Procesos de Revisión y Auditoría")
            .WithSummary("Recuperar la lista completa de sesiones de auditoría")
            .WithDescription("Obtiene un listado general del histórico y sesiones activas de revisiones.")
            .Produces<List<RevisionResponse>>(StatusCodes.Status200OK);
    }

    public record RevisionResponse(
        string Id, 
        string UsuarioId, 
        string Estado, 
        DateTime FechaInicio, 
        DateTime? FechaFin
    );

    private static IResult HandleAsync()
    {
        // TODO: Lógica futura
        // 1. Consultar base de datos
        // 2. Mapear entidades a RevisionResponse, encriptando Id y UsuarioId
        
        return Results.Ok(new List<RevisionResponse>());
    }
}
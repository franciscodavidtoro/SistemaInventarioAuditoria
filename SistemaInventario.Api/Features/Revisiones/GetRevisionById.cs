using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SistemaInventario.Api.Features.Revisiones;

public static class GetRevisionById
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // Recibe el string encriptado desde la URL
        app.MapGet("/api/revisiones/{id}", HandleAsync)
            .RequireAuthorization()
            .WithTags("Procesos de Revisión y Auditoría")
            .WithSummary("Obtener el detalle individual de una revisión")
            .Produces<GetRevisiones.RevisionResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static IResult HandleAsync(string id)
    {
        // TODO: Lógica futura
        // 1. Desencriptar el parámetro 'id' a Guid
        // 2. Buscar en EF Core
        // 3. Si no existe, return Results.NotFound()
        // 4. Mapear y retornar
        
        return Results.Ok(new GetRevisiones.RevisionResponse(id, "usuario_encriptado", "EnCurso", DateTime.UtcNow, null));
    }
}
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SistemaInventario.Api.Features.Revisiones;

public static class EscanearCodigo
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/revisiones/{id}/escanear", HandleAsync)
            .RequireAuthorization()
            .WithTags("Procesos de Revisión y Auditoría")
            .WithSummary("Procesar el escaneo de un código de barras físico")
            .WithDescription("Verifica existencia del ítem e intercepta duplicados con 409 Conflict.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);
    }

    public record EscaneoRequest(string CodigoBarras);

    private static IResult HandleAsync(string id, EscaneoRequest request)
    {
        // TODO: Lógica futura
        // 1. Desencriptar 'id' de la revisión a Guid
        // 2. Verificar que la revisión exista y esté "EnCurso" (sino 400)
        // 3. Buscar CodigoBarras en maestro de Elementos (sino 404)
        // 4. Verificar duplicidad en RevisionDetalles (si duplicado, 409)
        // 5. Insertar en RevisionDetalles
        
        return Results.Ok();
    }
}
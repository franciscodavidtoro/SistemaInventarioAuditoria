using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SistemaInventario.Api.Features.Revisiones;

public static class CrearRevision
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/revisiones", HandleAsync)
            .RequireAuthorization() // Exige token JWT
            .WithTags("Procesos de Revisión y Auditoría")
            .WithSummary("Inicializar una sesión de auditoría física de inventario")
            .WithDescription("Crea una cabecera de auditoría en la tabla maestro con estado inicial 'EnCurso'.")
            .Produces<Response>(StatusCodes.Status201Created);
    }

    // Devuelve el ID de la revisión (se encriptará antes de salir)
    public record Response(string Id);

    private static IResult HandleAsync()
    {
        // TODO: Lógica futura
        // 1. Extraer UsuarioId del JWT
        // 2. Crear entidad Revision (Estado = EnCurso, FechaInicio = Now)
        // 3. Guardar en EF Core
        // 4. Encriptar el Guid resultante y retornarlo
        
        return Results.Created("/api/revisiones/dummy-id", new Response("uuid_encriptado_aqui"));
    }
}
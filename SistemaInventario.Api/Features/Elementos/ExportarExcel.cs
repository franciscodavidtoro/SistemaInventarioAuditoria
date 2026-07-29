using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MiniExcelLibs;
using SistemaInventario.Api.Infrastructure.Database;

namespace SistemaInventario.Api.Features.Elementos;

public class ExportarExcelRequest
{
    public string? Buscar { get; set; }
}

public class ExportarExcelResponse { }

public static class ExportarExcelEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/elementos/exportar", async (string? buscar, ExportarExcelHandler handler) => await handler.HandleAsync(buscar))
            .RequireAuthorization()
            .WithTags("Procesamiento Masivo")
            .WithSummary("Exportar catálogo de elementos a archivo Excel")
            .WithDescription("Genera un archivo Excel con el inventario filtrado por nombre del bien o código del bien.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);
    }
}

public class ExportarExcelHandler
{
    private readonly ApplicationDbContext _db;

    public ExportarExcelHandler(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IResult> HandleAsync(string? buscar)
    {
        buscar = buscar?.Trim();

        var query = _db.Elementos.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            query = query.Where(e => e.NombreBien.Contains(buscar) || e.CodigoBien.Contains(buscar));
        }

        var elementos = await query
            .Select(e => new
            {
                e.Id,
                e.CodigoBien,
                e.NombreBien,
                e.Serie,
                e.Modelo,
                e.MarcaRazaOtros,
                e.Ubicacion,
                e.RutaImagen,
                e.UsuarioIdPropietario
            })
            .ToListAsync();

        var stream = new MemoryStream();
        string nombreArchivo = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        await MiniExcel.SaveAsAsync(stream, elementos, true, nombreArchivo, ExcelType.XLSX, null, CancellationToken.None);
        stream.Position = 0; ;

        return Results.File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "elementos.xlsx");
    }
}

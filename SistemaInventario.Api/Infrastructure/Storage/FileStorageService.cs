using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace SistemaInventario.Api.Infrastructure.Storage;

public class FileStorageService : IFileStorageService
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private static readonly string[] AllowedExtensions =
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".docx", ".xlsx", ".csv"
    };

    private static readonly string[] AllowedMimeTypes =
    {
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/csv"
    };

    private readonly string _contentRootPath;
    private readonly string _archivosRelativePath;

    public FileStorageService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _contentRootPath = environment.ContentRootPath;
        _archivosRelativePath = configuration.GetValue<string>("FileStorage:ArchivosPath")?.Trim() ?? "wwwroot/archivos/";
    }

    public long TamanioMaximoBytes => MaxFileSizeBytes;

    public bool EsExtensionValida(string extension) =>
        AllowedExtensions.Contains(extension.ToLowerInvariant());

    public bool EsMimeTypeValido(string mimeType) =>
        AllowedMimeTypes.Contains(mimeType.ToLowerInvariant());

    public async Task<(string NombreFisico, string Extension, string RutaRelativa)> GuardarAsync(IFormFile archivo)
    {
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        var nombreFisico = $"{Guid.NewGuid()}{extension}";

        var carpetaAbsoluta = Path.GetFullPath(_archivosRelativePath, _contentRootPath);
        Directory.CreateDirectory(carpetaAbsoluta);

        var rutaAbsoluta = Path.Combine(carpetaAbsoluta, nombreFisico);
        var rutaRelativa = Path.Combine(_archivosRelativePath, nombreFisico).Replace('\\', '/');

        await using (var destino = new FileStream(rutaAbsoluta, FileMode.Create, FileAccess.Write))
        {
            await archivo.CopyToAsync(destino);
        }

        return (nombreFisico, extension, rutaRelativa);
    }

    public void Eliminar(string rutaRelativa)
    {
        var rutaAbsoluta = Path.GetFullPath(rutaRelativa, _contentRootPath);
        if (File.Exists(rutaAbsoluta))
        {
            File.Delete(rutaAbsoluta);
        }
    }
}

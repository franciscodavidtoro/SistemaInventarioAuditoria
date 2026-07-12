using Microsoft.AspNetCore.Http;

namespace SistemaInventario.Api.Infrastructure.Storage;

public interface IFileStorageService
{
    long TamanioMaximoBytes { get; }

    bool EsExtensionValida(string extension);

    bool EsMimeTypeValido(string mimeType);

    Task<(string NombreFisico, string Extension, string RutaRelativa)> GuardarAsync(IFormFile archivo);

    void Eliminar(string rutaRelativa);
}

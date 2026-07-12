using System;

namespace SistemaInventario.Api.Domain.Entities;

public class Archivo
{
    public Guid Id { get; set; }
    public string NombreOriginal { get; set; } = string.Empty;
    public string NombreFisico { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string TipoMime { get; set; } = string.Empty;
    public long TamanioBytes { get; set; }
    public string RutaRelativa { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public Guid UsuarioIdPropietario { get; set; }
}

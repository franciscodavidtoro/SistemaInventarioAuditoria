using System;

namespace SistemaInventario.Api.Domain.Entities;

public class Imagen
{
    public Guid Id { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string EntidadTipo { get; set; } = string.Empty;
    public Guid EntidadId { get; set; }
    public Guid UsuarioIdCarga { get; set; }
    public DateTime FechaCreacion { get; set; }
}

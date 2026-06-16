using Microsoft.EntityFrameworkCore;
using SistemaInventario.Api.Domain.Entities;

namespace SistemaInventario.Api.Infrastructure.Database;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios { get; set; } = null!;
    public DbSet<Elemento> Elementos { get; set; } = null!;
    public DbSet<Revision> Revisiones { get; set; } = null!;
    public DbSet<RevisionDetalle> RevisionDetalles { get; set; } = null!;
}

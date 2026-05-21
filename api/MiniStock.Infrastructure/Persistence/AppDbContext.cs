using Microsoft.EntityFrameworkCore;
using MiniStock.Application.Interfaces;
using MiniStock.Domain.Entities;

namespace MiniStock.Infrastructure.Persistence;

/// <summary>
/// Contexto de base de datos de la aplicación. Implementa <see cref="IUnitOfWork"/>
/// para exponer <c>SaveChangesAsync</c> a la capa de Application sin que ésta
/// dependa directamente de EF Core.
/// </summary>
/// <remarks>
/// <b>Por qué implementar IUnitOfWork en el DbContext:</b>
/// El patrón Unit of Work agrupa múltiples operaciones de repositorio en una sola
/// transacción. En EF Core, el DbContext ya ES una unidad de trabajo: acumula
/// cambios en memoria (change tracker) y los persiste todos juntos al llamar
/// <c>SaveChangesAsync</c>. En lugar de crear una capa extra, se implementa
/// la interfaz directamente para no duplicar la responsabilidad.
///
/// <b>Configuraciones separadas:</b> cada entidad tiene su propia clase
/// <c>IEntityTypeConfiguration&lt;T&gt;</c> en la carpeta <c>Configurations/</c>.
/// <see cref="OnModelCreating"/> las registra todas automáticamente con
/// <c>ApplyConfigurationsFromAssembly</c>, lo que mantiene el contexto limpio
/// y permite agregar nuevas entidades sin modificar este archivo.
/// </remarks>
public class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User>          Users          => Set<User>();
    public DbSet<Role>          Roles          => Set<Role>();
    public DbSet<Product>       Products       => Set<Product>();
    public DbSet<Category>      Categories     => Set<Category>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Registra todas las IEntityTypeConfiguration<T> del assembly de Infrastructure
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

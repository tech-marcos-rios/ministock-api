using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniStock.Domain.Entities;

namespace MiniStock.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    // GUIDs fijos para poder referenciarlos en seeds y tests sin consultar la DB
    public static readonly Guid AdminRoleId = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid UserRoleId  = new("00000000-0000-0000-0000-000000000002");

    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(50);
        builder.HasIndex(r => r.Name).IsUnique();

        builder.HasData(
            CreateRole(AdminRoleId, Role.Names.Admin),
            CreateRole(UserRoleId,  Role.Names.User)
        );
    }

    private static object CreateRole(Guid id, string name) => new
    {
        Id        = id,
        Name      = name,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = (DateTime?)null
    };
}

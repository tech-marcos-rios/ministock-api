namespace MiniStock.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; private set; } = default!;

    private Role() { }

    public static Role Create(string name) => new() { Name = name };

    public static class Names
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }

    /// <summary>
    /// GUIDs fijos de los roles seedeados, para poder referenciarlos en código (seeds, el rol
    /// por defecto de un usuario nuevo, tests) sin depender de un detalle de persistencia.
    /// </summary>
    public static class WellKnownIds
    {
        public static readonly Guid AdminRoleId = new("00000000-0000-0000-0000-000000000001");
        public static readonly Guid UserRoleId  = new("00000000-0000-0000-0000-000000000002");
    }
}

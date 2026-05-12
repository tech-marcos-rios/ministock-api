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
}

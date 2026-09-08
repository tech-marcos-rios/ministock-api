using MiniStock.Domain.Entities;

namespace MiniStock.Application.Common;

/// <summary>
/// Re-exporta los nombres de rol de Domain para que la capa Api pueda usarlos en
/// atributos <c>[Authorize(Roles = ...)]</c> (que exigen una constante de compilación)
/// sin referenciar <c>MiniStock.Domain</c> directamente — Api solo puede depender de
/// Application (ver la regla de dependencia en el CLAUDE.md del proyecto).
/// </summary>
public static class RoleNames
{
    public const string Admin = Role.Names.Admin;
    public const string User = Role.Names.User;
}

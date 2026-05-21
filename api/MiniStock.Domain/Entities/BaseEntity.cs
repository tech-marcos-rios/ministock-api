namespace MiniStock.Domain.Entities;

/// <summary>
/// Clase base para todas las entidades del dominio.
/// </summary>
/// <remarks>
/// Centraliza las tres propiedades que toda entidad necesita:
/// identidad (<see cref="Id"/>), auditoría de creación (<see cref="CreatedAt"/>)
/// y auditoría de modificación (<see cref="UpdatedAt"/>).
///
/// Los setters son <c>protected</c> para garantizar que solo el propio tipo
/// o sus subclases puedan modificar estos valores — EF Core los puede
/// inicializar vía reflexión, pero el código de aplicación no puede hacerlo
/// directamente (encapsulación del dominio).
///
/// Se usa <see cref="Guid"/> en lugar de <c>int</c> autoincremental porque:
/// - Permite generar el Id en memoria antes de persistir, sin ir a la BD.
/// - Es seguro para arquitecturas distribuidas o multi-tenant.
/// - Evita exponer información de volumen de registros en la URL.
///
/// Todas las fechas son UTC para evitar ambigüedades de zona horaria.
/// </remarks>
public abstract class BaseEntity
{
    /// <summary>Identificador único, generado en memoria al construir la entidad.</summary>
    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>Fecha y hora UTC en que se creó el registro.</summary>
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    /// <summary>Fecha y hora UTC de la última modificación. <c>null</c> si nunca fue actualizado.</summary>
    public DateTime? UpdatedAt { get; protected set; }

    /// <summary>
    /// Establece <see cref="UpdatedAt"/> al momento actual UTC.
    /// Debe llamarse al final de cada método de mutación de la entidad.
    /// </summary>
    protected void SetUpdatedAt() => UpdatedAt = DateTime.UtcNow;
}

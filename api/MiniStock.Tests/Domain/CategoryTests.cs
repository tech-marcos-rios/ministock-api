using FluentAssertions;
using MiniStock.Domain.Entities;

namespace MiniStock.Tests.Domain;

public class CategoryTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var category = Category.Create("Electrónica", "Dispositivos");
        category.Name.Should().Be("Electrónica");
        category.Description.Should().Be("Dispositivos");
        category.IsActive.Should().BeTrue();
        category.Id.Should().NotBeEmpty();
        category.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithoutDescription_IsNull()
    {
        var category = Category.Create("Herramientas");
        category.Description.Should().BeNull();
    }

    [Fact]
    public void Update_ChangesNameAndDescription()
    {
        var category = Category.Create("Viejo", "Desc vieja");
        category.Update("Nuevo", "Desc nueva");
        category.Name.Should().Be("Nuevo");
        category.Description.Should().Be("Desc nueva");
        category.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var category = Category.Create("Test");
        category.Deactivate();
        category.IsActive.Should().BeFalse();
        category.UpdatedAt.Should().NotBeNull();
    }
}

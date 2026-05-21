using FluentAssertions;
using MiniStock.Application.DTOs.Auth;
using MiniStock.Application.DTOs.Categories;
using MiniStock.Application.DTOs.Products;
using MiniStock.Application.DTOs.StockMovements;
using MiniStock.Application.Validators.Auth;
using MiniStock.Application.Validators.Categories;
using MiniStock.Application.Validators.Products;
using MiniStock.Application.Validators.StockMovements;
using MiniStock.Domain.Entities;

namespace MiniStock.Tests.Application;

public class RegisterValidatorTests
{
    private readonly RegisterValidator _validator = new();

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var result = _validator.Validate(new RegisterRequest("Marcos", "marcos@test.com", "Password1!"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyName_FailsValidation()
    {
        var result = _validator.Validate(new RegisterRequest("", "marcos@test.com", "Password1!"));
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void InvalidEmail_FailsValidation()
    {
        var result = _validator.Validate(new RegisterRequest("Marcos", "not-an-email", "Password1!"));
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void PasswordTooShort_FailsValidation()
    {
        var result = _validator.Validate(new RegisterRequest("Marcos", "m@test.com", "Ab1"));
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void PasswordNoUppercase_FailsValidation()
    {
        var result = _validator.Validate(new RegisterRequest("Marcos", "m@test.com", "password1!"));
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void PasswordNoNumber_FailsValidation()
    {
        var result = _validator.Validate(new RegisterRequest("Marcos", "m@test.com", "Password!"));
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }
}

public class CreateProductValidatorTests
{
    private readonly CreateProductValidator _validator = new();

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var result = _validator.Validate(new CreateProductRequest("Laptop", "LAP-001", 999m, 10, 3, Guid.NewGuid(), null));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ZeroPrice_FailsValidation()
    {
        var result = _validator.Validate(new CreateProductRequest("Laptop", "LAP-001", 0m, 10, 3, Guid.NewGuid(), null));
        result.Errors.Should().Contain(e => e.PropertyName == "Price");
    }

    [Fact]
    public void NegativeInitialStock_FailsValidation()
    {
        var result = _validator.Validate(new CreateProductRequest("Laptop", "LAP-001", 100m, -1, 3, Guid.NewGuid(), null));
        result.Errors.Should().Contain(e => e.PropertyName == "InitialStock");
    }

    [Fact]
    public void EmptyCategoryId_FailsValidation()
    {
        var result = _validator.Validate(new CreateProductRequest("Laptop", "LAP-001", 100m, 5, 3, Guid.Empty, null));
        result.Errors.Should().Contain(e => e.PropertyName == "CategoryId");
    }

    [Fact]
    public void EmptySku_FailsValidation()
    {
        var result = _validator.Validate(new CreateProductRequest("Laptop", "", 100m, 5, 3, Guid.NewGuid(), null));
        result.Errors.Should().Contain(e => e.PropertyName == "SKU");
    }
}

public class RegisterMovementValidatorTests
{
    private readonly RegisterMovementValidator _validator = new();

    [Fact]
    public void ValidEntry_PassesValidation()
    {
        var result = _validator.Validate(new RegisterMovementRequest(Guid.NewGuid(), 5, MovementType.Entry, null));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ExitWithZeroQuantity_FailsValidation()
    {
        var result = _validator.Validate(new RegisterMovementRequest(Guid.NewGuid(), 0, MovementType.Exit, null));
        result.Errors.Should().Contain(e => e.PropertyName == "Quantity");
    }

    [Fact]
    public void AdjustmentWithZeroQuantity_FailsValidation()
    {
        var result = _validator.Validate(new RegisterMovementRequest(Guid.NewGuid(), 0, MovementType.Adjustment, null));
        result.Errors.Should().Contain(e => e.PropertyName == "Quantity");
    }

    [Fact]
    public void AdjustmentWithNegativeQuantity_PassesValidation()
    {
        var result = _validator.Validate(new RegisterMovementRequest(Guid.NewGuid(), -3, MovementType.Adjustment, null));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void NotesTooLong_FailsValidation()
    {
        var longNotes = new string('x', 301);
        var result = _validator.Validate(new RegisterMovementRequest(Guid.NewGuid(), 1, MovementType.Entry, longNotes));
        result.Errors.Should().Contain(e => e.PropertyName == "Notes");
    }

    [Fact]
    public void EmptyProductId_FailsValidation()
    {
        var result = _validator.Validate(new RegisterMovementRequest(Guid.Empty, 1, MovementType.Entry, null));
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId");
    }
}

public class CreateCategoryValidatorTests
{
    private readonly CreateCategoryValidator _validator = new();

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var result = _validator.Validate(new CreateCategoryRequest("Electrónica", "Dispositivos"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyName_FailsValidation()
    {
        var result = _validator.Validate(new CreateCategoryRequest("", null));
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void NameTooLong_FailsValidation()
    {
        var longName = new string('x', 101);
        var result = _validator.Validate(new CreateCategoryRequest(longName, null));
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }
}

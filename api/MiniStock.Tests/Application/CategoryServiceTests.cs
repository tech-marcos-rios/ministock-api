using FluentAssertions;
using MiniStock.Application.DTOs.Categories;
using MiniStock.Application.Interfaces;
using MiniStock.Application.Services;
using MiniStock.Application.Common;
using MiniStock.Domain.Entities;
using Moq;

namespace MiniStock.Tests.Application;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IProductRepository>  _products   = new();
    private readonly Mock<IUnitOfWork>         _uow        = new();
    private readonly CategoryService           _sut;

    public CategoryServiceTests()
    {
        _sut = new CategoryService(_categories.Object, _products.Object, _uow.Object);
    }

    [Fact]
    public async Task Create_UniqueName_ReturnsSuccess()
    {
        _categories.Setup(r => r.ExistsByNameAsync("Electrónica", default)).ReturnsAsync(false);
        _categories.Setup(r => r.AddAsync(It.IsAny<Category>(), default)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _sut.CreateAsync(new CreateCategoryRequest("Electrónica", "Desc"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Electrónica");
        result.Value.ProductCount.Should().Be(0);
    }

    [Fact]
    public async Task Create_DuplicateName_ReturnsFailure()
    {
        _categories.Setup(r => r.ExistsByNameAsync("Electrónica", default)).ReturnsAsync(true);

        var result = await _sut.CreateAsync(new CreateCategoryRequest("Electrónica", null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Electrónica");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_SameName_SkipsUniqueCheck()
    {
        var category = Category.Create("Electrónica", "Desc");
        _categories.Setup(r => r.GetByIdAsync(category.Id, default)).ReturnsAsync(category);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Misma nombre — no debe llamar ExistsByName
        var result = await _sut.UpdateAsync(category.Id, new UpdateCategoryRequest("Electrónica", "Nueva desc"));

        result.IsSuccess.Should().BeTrue();
        _categories.Verify(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_DifferentNameAlreadyExists_ReturnsFailure()
    {
        var category = Category.Create("Original");
        _categories.Setup(r => r.GetByIdAsync(category.Id, default)).ReturnsAsync(category);
        _categories.Setup(r => r.ExistsByNameAsync("Ocupado", default)).ReturnsAsync(true);

        var result = await _sut.UpdateAsync(category.Id, new UpdateCategoryRequest("Ocupado", null));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Deactivate_ExistingCategory_Succeeds()
    {
        var category = Category.Create("Test");
        _categories.Setup(r => r.GetByIdAsync(category.Id, default)).ReturnsAsync(category);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _sut.DeactivateAsync(category.Id);

        result.IsSuccess.Should().BeTrue();
        category.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetPaged_NormalizesPageBelowOne()
    {
        var paged = new PagedResult<Category>([], 0, 1, 20);
        _categories.Setup(r => r.GetPagedAsync(1, 20, null, default)).ReturnsAsync(paged);

        await _sut.GetPagedAsync(page: 0, pageSize: 20, null);

        _categories.Verify(r => r.GetPagedAsync(1, 20, null, default), Times.Once);
    }
}

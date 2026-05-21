using FluentAssertions;
using MiniStock.Application.Common;

namespace MiniStock.Tests.Application;

public class ResultTests
{
    [Fact]
    public void Success_IsSuccessTrue_IsFailureFalse_ErrorNull()
    {
        var result = Result.Success();
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_IsSuccessFalse_IsFailureTrue_HasError()
    {
        var result = Result.Failure("Algo falló");
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Algo falló");
    }

    [Fact]
    public void SuccessT_CarriesValue()
    {
        var result = Result.Success(42);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void FailureT_ValueIsDefault()
    {
        var result = Result.Failure<int>("error");
        result.IsFailure.Should().BeTrue();
        result.Value.Should().Be(default);
    }

    [Fact]
    public void SuccessT_StringValue_CarriesString()
    {
        var result = Result.Success("hola");
        result.Value.Should().Be("hola");
    }
}

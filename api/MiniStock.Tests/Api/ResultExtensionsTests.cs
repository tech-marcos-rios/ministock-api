using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using MiniStock.Api.Extensions;
using MiniStock.Application.Common;

namespace MiniStock.Tests.Api;

public class ResultExtensionsTests
{
    [Fact]
    public void ToActionResult_SuccessT_ReturnsOkWithValue()
    {
        var result = Result.Success("hola");

        var action = result.ToActionResult();

        var ok = action.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be("hola");
    }

    [Fact]
    public void ToActionResult_Success_ReturnsNoContent()
    {
        var result = Result.Success();

        var action = result.ToActionResult();

        action.Should().BeOfType<NoContentResult>();
    }

    [Theory]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Validation, 400)]
    public void ToActionResult_FailureT_MapsErrorTypeToStatusCode(ErrorType type, int expectedStatus)
    {
        var result = Result.Failure<string>("algo falló", type);

        var action = result.ToActionResult();

        var problemResult = action.Should().BeOfType<ObjectResult>().Subject;
        problemResult.StatusCode.Should().Be(expectedStatus);
        var problem = problemResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Detail.Should().Be("algo falló");
        problem.Status.Should().Be(expectedStatus);
    }

    [Fact]
    public void ToActionResult_WithOnSuccess_Failure_StillMapsError()
    {
        var result = Result.Failure<string>("categoría no encontrada", ErrorType.NotFound);

        var action = result.ToActionResult(value => new OkObjectResult(value));

        var problemResult = action.Should().BeOfType<ObjectResult>().Subject;
        problemResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public void ToActionResult_WithOnSuccess_Success_UsesCallback()
    {
        var result = Result.Success("creado");

        var action = result.ToActionResult(value => new CreatedResult("/products/1", value));

        var created = action.Should().BeOfType<CreatedResult>().Subject;
        created.Value.Should().Be("creado");
    }
}

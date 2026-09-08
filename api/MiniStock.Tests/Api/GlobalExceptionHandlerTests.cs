using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using MiniStock.Api;
using Moq;

namespace MiniStock.Tests.Api;

public class GlobalExceptionHandlerTests
{
    private static (GlobalExceptionHandler handler, DefaultHttpContext context) BuildHandler(bool isDevelopment)
    {
        var env = new Mock<IHostEnvironment>();
        env.Setup(e => e.EnvironmentName).Returns(isDevelopment ? Environments.Development : Environments.Production);

        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, env.Object);
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        return (handler, context);
    }

    [Fact]
    public async Task TryHandleAsync_ReturnsTrue_AndWrites500ProblemDetails()
    {
        var (handler, context) = BuildHandler(isDevelopment: false);

        var handled = await handler.TryHandleAsync(context, new InvalidOperationException("boom"), default);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        var problem = await ReadProblemAsync(context);
        problem!.Status.Should().Be(500);
        problem.Title.Should().Be("An unexpected error occurred.");
    }

    [Fact]
    public async Task TryHandleAsync_Production_DoesNotLeakExceptionDetails()
    {
        var (handler, context) = BuildHandler(isDevelopment: false);

        await handler.TryHandleAsync(context, new InvalidOperationException("secret internals"), default);

        var problem = await ReadProblemAsync(context);
        problem!.Detail.Should().BeNull();
    }

    [Fact]
    public async Task TryHandleAsync_Development_IncludesExceptionDetails()
    {
        var (handler, context) = BuildHandler(isDevelopment: true);

        await handler.TryHandleAsync(context, new InvalidOperationException("secret internals"), default);

        var problem = await ReadProblemAsync(context);
        problem!.Detail.Should().Contain("secret internals");
    }

    private static async Task<ProblemDetails?> ReadProblemAsync(DefaultHttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await JsonSerializer.DeserializeAsync<ProblemDetails>(
            context.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}

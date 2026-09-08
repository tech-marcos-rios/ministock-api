using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MiniStock.Api.Filters;

namespace MiniStock.Tests.Api;

public class ValidationFilterTests
{
    private record Dummy(string Name);

    private class DummyValidator : AbstractValidator<Dummy>
    {
        public DummyValidator() => RuleFor(x => x.Name).NotEmpty();
    }

    private static ActionExecutingContext BuildContext(object? argument, IServiceProvider services)
    {
        var httpContext = new DefaultHttpContext { RequestServices = services };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        var arguments = new Dictionary<string, object?>();
        if (argument is not null) arguments["request"] = argument;

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            arguments!,
            controller: new object());
    }

    private static IServiceProvider BuildServices() =>
        new ServiceCollection().AddTransient<IValidator<Dummy>, DummyValidator>().BuildServiceProvider();

    [Fact]
    public async Task InvalidArgument_ShortCircuitsWith400()
    {
        var context = BuildContext(new Dummy(""), BuildServices());
        var filter = new ValidationFilter();
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        nextCalled.Should().BeFalse();
        var badRequest = context.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeOfType<ValidationProblemDetails>();
    }

    [Fact]
    public async Task ValidArgument_CallsNext()
    {
        var context = BuildContext(new Dummy("Laptop"), BuildServices());
        var filter = new ValidationFilter();
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        nextCalled.Should().BeTrue();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task ArgumentWithoutValidator_CallsNext()
    {
        // "not-a-dummy" no tiene IValidator<string> registrado — el filtro debe ignorarlo.
        var context = BuildContext("not-a-dummy", BuildServices());
        var filter = new ValidationFilter();
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        nextCalled.Should().BeTrue();
    }
}

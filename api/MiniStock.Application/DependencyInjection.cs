using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MiniStock.Application.Interfaces;
using MiniStock.Application.Services;

namespace MiniStock.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IStockMovementService, StockMovementService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}

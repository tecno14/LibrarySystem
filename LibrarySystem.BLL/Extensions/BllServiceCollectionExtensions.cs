using LibrarySystem.BLL.Development;
using LibrarySystem.BLL.Initialization;
using LibrarySystem.BLL.Interfaces;
using LibrarySystem.BLL.Services;
using LibrarySystem.DAL.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace LibrarySystem.BLL.Extensions;

public static class BllServiceCollectionExtensions
{
    public static IServiceCollection AddBllServices(this IServiceCollection services)
    {
        services.AddTransient<AdminAccountInitializer>();

        // Register DAL dependencies
        services.AddDalServices();

        // Register BLL services
        services.AddMemoryCache(); // Adds the in-memory caching service.
        services.AddScoped<ICacheService, MemoryCacheService>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ISampleDataSeeder, SampleDataSeeder>(); // For seeding fake data in development.

        return services;
    }

    public static async Task InitializeBllServicesAsync(this IServiceProvider serviceProvider)
    {
        // This block create the default admin user if there is no any user exist.
        await SeedAdminAccountAsync(serviceProvider);
    }

    private static async Task SeedAdminAccountAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        var seeder = services.GetRequiredService<AdminAccountInitializer>();
        await seeder.SeedAdminUserAsync();
    }
}

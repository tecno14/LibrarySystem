using LibrarySystem.DAL.Connections;
using LibrarySystem.DAL.Interfaces;
using LibrarySystem.DAL.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace LibrarySystem.DAL.Extensions;

public static class DalServiceCollectionExtensions
{
    public static IServiceCollection AddDalServices(this IServiceCollection services)
    {
        // Register DAL dependencies
        services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IBookCopyRepository, BookCopyRepository>();
        services.AddScoped<IBorrowingRepository, BorrowingRepository>();

        return services;
    }
}

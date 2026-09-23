using HelpDesk.Application.Interfaces;
using HelpDesk.Domain.Entities;
using HelpDesk.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HelpDesk.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (dbContext.Database.IsSqlite())
        {
            await dbContext.Database.EnsureCreatedAsync();
        }
        else
        {
            await dbContext.Database.MigrateAsync();
        }

        await SeedAdministratorAsync(scope.ServiceProvider, dbContext);
    }

    private static async Task SeedAdministratorAsync(
        IServiceProvider scopedProvider,
        AppDbContext dbContext)
    {
        var configuration = scopedProvider.GetRequiredService<IConfiguration>();

        var adminEmail = configuration["Seed:AdminEmail"] ?? "admin@helpdesk.local";

        var adminExists = await dbContext.Users
            .AnyAsync(u => u.Email == adminEmail);

        if (adminExists)
        {
            return;
        }

        var passwordHasher = scopedProvider.GetRequiredService<IPasswordHasher>();

        var admin = new User
        {
            FirstName = configuration["Seed:AdminFirstName"] ?? "System",
            LastName = configuration["Seed:AdminLastName"] ?? "Administrator",
            Email = adminEmail,
            PasswordHash = passwordHasher.Hash(
                configuration["Seed:AdminPassword"] ?? "Admin@123"),
            Role = UserRole.Admin,
            DepartmentId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(admin);
        await dbContext.SaveChangesAsync();
    }
}

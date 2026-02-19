using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResultViewer.Application.Interfaces;
using ResultViewer.Application.Services;
using ResultViewer.Domain.Interfaces;
using ResultViewer.Infrastructure.Data;
using ResultViewer.Infrastructure.Repositories;
using ResultViewer.Infrastructure.Services;

namespace ResultViewer.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // EF Core / SQL Server
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("SqlServer"),
                sql =>
                {
                    sql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name);
                    sql.CommandTimeout(120);
                }));

        // Repositories
        services.AddScoped<ITestRunRepository, TestRunRepository>();

        // Domain services
        services.AddScoped<IFileSystemService, FileSystemService>();
        services.AddScoped<IXmlParserService, XmlParserService>();

        // Application services
        services.AddScoped<IRunQueryService, RunQueryService>();
        services.AddScoped<IRetentionService, RetentionService>();

        // Memory cache
        services.AddMemoryCache();

        return services;
    }
}

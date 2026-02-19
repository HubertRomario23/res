using Microsoft.EntityFrameworkCore;
using ResultViewer.Api.Middleware;
using ResultViewer.Infrastructure;
using ResultViewer.Infrastructure.Data;
using Serilog;

// ---------- Bootstrap Serilog ----------
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog from appsettings
    builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

    // Controllers + Swagger
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "ResultViewer API",
            Version = "v1",
            Description = "Hybrid ingestion/retrieval API for test run results."
        });
    });

    // Infrastructure (EF Core, repositories, services, caching)
    builder.Services.AddInfrastructure(builder.Configuration);

    // Health checks
    builder.Services.AddHealthChecks()
        .AddSqlServer(builder.Configuration.GetConnectionString("SqlServer")!);

    // CORS
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod());
    });

    var app = builder.Build();

    // ---------- Auto-migrate on startup ----------
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Log.Information("Applying pending EF Core migrations…");
        await db.Database.MigrateAsync();
        Log.Information("Database migration complete.");
    }

    // ---------- Middleware pipeline ----------
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<GlobalExceptionMiddleware>();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseCors();
    app.MapControllers();
    app.MapHealthChecks("/health");

    Log.Information("ResultViewer API starting on {Urls}", string.Join(", ", builder.WebHost.GetSetting("urls") ?? "default"));
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

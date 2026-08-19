using Microsoft.EntityFrameworkCore;
using MnemoToad.Learning.Api.Swagger;
using MnemoToad.Learning.Data;
using MnemoToad.Learning.Data.Configuration;

namespace MnemoToad.Learning.Api.Configuration;

public static class ApiServiceRegistration
{
    public static IServiceCollection AddApiServices(IServiceCollection services, IConfiguration configuration)
    {
        // Wires up routing/model-binding/action-invocation for [ApiController] classes.
        services.AddControllers();

        // Lets Swashbuckle discover our controllers' routes/parameters/response types.
        services.AddEndpointsApiExplorer();
        // Registers the OpenAPI document generator (built from the explorer data above).
        // Nothing is written to disk here — the JSON is generated in memory per-request by
        // app.UseSwagger() below. Configuration lives in SwaggerGenOptionsSetup.
        services.ConfigureOptions<SwaggerGenOptionsSetup>();
        services.AddSwaggerGen();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")).UseSnakeCaseNamingConvention());
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("Default")!, name: "database");

        DataServiceRegistration.AddDataServices(services);

        return services;
    }
}

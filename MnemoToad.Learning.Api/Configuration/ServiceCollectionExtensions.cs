using Microsoft.EntityFrameworkCore;
using MnemoToad.Learning.Data;

namespace MnemoToad.Learning.Api.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Wires up routing/model-binding/action-invocation for [ApiController] classes.
        services.AddControllers();

        // Lets Swashbuckle discover our controllers' routes/parameters/response types.
        services.AddEndpointsApiExplorer();
        // Registers the OpenAPI document generator (built from the explorer data above).
        // Nothing is written to disk here — the JSON is generated in memory per-request by
        // app.UseSwagger() below, only when running in Development.
        services.AddSwaggerGen();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")).UseSnakeCaseNamingConvention());
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using MnemoToad.Learning.Data.Repositories;

namespace MnemoToad.Learning.Data.Configuration;

public static class DataServiceRegistration
{
    public static IServiceCollection AddDataServices(IServiceCollection services)
    {
        services.AddScoped<ILeitnerDeckRepository, LeitnerDeckRepository>();
        return services;
    }
}

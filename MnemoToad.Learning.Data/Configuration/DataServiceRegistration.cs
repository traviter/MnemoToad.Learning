using Microsoft.Extensions.DependencyInjection;
using MnemoToad.Learning.Data.DbUtil;
using MnemoToad.Learning.Data.Entities;
using MnemoToad.Learning.Data.Entities.Operations;
using MnemoToad.Learning.Data.Repositories;

namespace MnemoToad.Learning.Data.Configuration;

public static class DataServiceRegistration
{
    public static IServiceCollection AddDataServices(IServiceCollection services)
    {
        services.AddSingleton<IEntityJsonMapper<LeitnerCardFace>, LeitnerCardFaceJsonMapper>();
        services.AddSingleton<IJsonMapper<LeitnerCardFace>>(sp => sp.GetRequiredService<IEntityJsonMapper<LeitnerCardFace>>());
        services.AddSingleton<IJsonMapper<IEnumerable<LeitnerCardFace>>, CompositeJsonMapper<LeitnerCardFace>>();
        services.AddScoped<ILeitnerCardFaceRepository, LeitnerCardFaceRepository>();
        services.AddScoped<ILeitnerDeckRepository, LeitnerDeckRepository>();
        services.AddScoped<ILeitnerCardRepository, LeitnerCardRepository>();
        services.AddScoped<ILeitnerQuizRepository, LeitnerQuizRepository>();
        return services;
    }
}

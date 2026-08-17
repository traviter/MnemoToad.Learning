using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MnemoToad.Learning.Api.Swagger;

public class SwaggerGenOptionsSetup : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "MnemoToad Learning API",
            Version = "v1",
            Description = "A spaced-repetition flashcard learning service.",
        });

        // GenerateDocumentationFile in both this project and MnemoToad.Learning.Data (entities
        // are returned directly as responses, no separate response DTOs) drives these files.
        options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "MnemoToad.Learning.Api.xml"));
        options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "MnemoToad.Learning.Data.xml"));
    }
}

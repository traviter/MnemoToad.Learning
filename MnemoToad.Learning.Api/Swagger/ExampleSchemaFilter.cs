using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using MnemoToad.Learning.Api.Contracts;
using MnemoToad.Learning.Data.Entities;

namespace MnemoToad.Learning.Api.Swagger;

// Attaches a sample JSON payload to specific request/response types in the generated Swagger
// schema. Add an entry here (not a per-type attribute/class) whenever a new type needs one.
public class ExampleSchemaFilter : ISchemaFilter
{
    private static readonly Dictionary<Type, Func<JsonNode>> Examples = new()
    {
        [typeof(LeitnerDeckRequest)] = () => new JsonObject
        {
            ["name"] = "World Capitals",
            ["description"] = "Country name on the front, capital city on the back.",
        },
        [typeof(LeitnerDeck)] = () => new JsonObject
        {
            ["id"] = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            ["name"] = "World Capitals",
            ["description"] = "Country name on the front, capital city on the back.",
        },
    };

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is OpenApiSchema concrete && Examples.TryGetValue(context.Type, out var example))
        {
            concrete.Example = example();
        }
    }
}

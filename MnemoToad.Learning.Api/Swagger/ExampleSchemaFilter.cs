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
        [typeof(LeitnerCardsBulkCreateRequest)] = () => new JsonObject
        {
            ["deckId"] = "8b1e2c3d-4a5b-6c7d-8e9f-0a1b2c3d4e5f",
            ["cards"] = new JsonArray(
                new JsonObject
                {
                    ["nodeId"] = "7c2d3e4f-5a6b-4c7d-8e9f-0a1b2c3d4e5f",
                    ["properties"] = new JsonObject
                    {
                        ["_canonicalName"] = "France",
                        [".population"] = 68000000,
                        ["#flag"] = new JsonObject
                        {
                            ["id"] = "a1b2c3d4-e5f6-4789-9abc-def012345678",
                            ["alt_text"] = "The flag of France",
                        },
                    },
                },
                new JsonObject
                {
                    ["properties"] = new JsonObject
                    {
                        ["_canonicalName"] = "Japan",
                        [".population"] = 123000000,
                    },
                }),
        },
        [typeof(LeitnerCardsBulkCreateResponse)] = () => new JsonObject
        {
            ["cards"] = new JsonArray(
                new JsonObject
                {
                    ["id"] = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                    ["deckId"] = "8b1e2c3d-4a5b-6c7d-8e9f-0a1b2c3d4e5f",
                    ["nodeId"] = "7c2d3e4f-5a6b-4c7d-8e9f-0a1b2c3d4e5f",
                    ["boxNumber"] = 0,
                    ["dueUtc"] = "2026-08-19T14:32:00Z",
                    ["lastReviewedUtc"] = null,
                    ["properties"] = new JsonObject
                    {
                        ["_canonicalName"] = "France",
                        [".population"] = 68000000,
                        ["#flag"] = new JsonObject
                        {
                            ["id"] = "a1b2c3d4-e5f6-4789-9abc-def012345678",
                            ["alt_text"] = "The flag of France",
                        },
                    },
                },
                new JsonObject
                {
                    ["id"] = "d4e5f6a7-8b9c-4d0e-9f1a-2b3c4d5e6f70",
                    ["deckId"] = "8b1e2c3d-4a5b-6c7d-8e9f-0a1b2c3d4e5f",
                    ["nodeId"] = null,
                    ["boxNumber"] = 0,
                    ["dueUtc"] = "2026-08-19T14:32:00Z",
                    ["lastReviewedUtc"] = null,
                    ["properties"] = new JsonObject
                    {
                        ["_canonicalName"] = "Japan",
                        [".population"] = 123000000,
                    },
                }),
        },
        [typeof(LeitnerCard)] = () => new JsonObject
        {
            ["id"] = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            ["deckId"] = "8b1e2c3d-4a5b-6c7d-8e9f-0a1b2c3d4e5f",
            ["nodeId"] = "7c2d3e4f-5a6b-4c7d-8e9f-0a1b2c3d4e5f",
            ["boxNumber"] = 0,
            ["dueUtc"] = "2026-08-19T14:32:00Z",
            ["lastReviewedUtc"] = null,
            ["properties"] = new JsonObject
            {
                ["_canonicalName"] = "France",
                [".population"] = 68000000,
                ["#flag"] = new JsonObject
                {
                    ["id"] = "a1b2c3d4-e5f6-4789-9abc-def012345678",
                    ["alt_text"] = "The flag of France",
                },
            },
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

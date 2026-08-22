using MnemoToad.Learning.Data.DbUtil;
using MnemoToad.Learning.Data.Entities;
using MnemoToad.Learning.Data.Entities.Operations;
using NUnit.Framework;
using System.Text.Json.Nodes;

namespace MnemoToad.Learning.Tests.DbUtil;

[TestFixture]
public class CompositeJsonMapperTests
{
    private readonly CompositeJsonMapper<LeitnerCardFace> _mapper = new(new LeitnerCardFaceJsonMapper());

    [Test]
    public void ToJson_MergesEntitiesInInputOrder()
    {
        var faces = new List<LeitnerCardFace>
        {
            new() { PropertyPath = "#flag", Content = JsonValue.Create("flag-value") },
            new() { PropertyPath = "_canonicalName", Content = JsonValue.Create("France") },
            new() { PropertyPath = ".population", Content = JsonValue.Create(68000000) }
        };

        var json = _mapper.ToJson(faces);

        Assert.That(json.Select(p => p.Key), Is.EqualTo(new[] { "#flag", "_canonicalName", ".population" }));
    }

    [Test]
    public void ToJson_WithEmptyInput_ReturnsEmptyObject()
    {
        var json = _mapper.ToJson(new List<LeitnerCardFace>());

        Assert.That(json.Count, Is.EqualTo(0));
    }

    [Test]
    public void ToJson_ResultDoesNotAliasSourceEntities()
    {
        var content = new JsonObject { ["id"] = Guid.NewGuid().ToString() };
        var faces = new List<LeitnerCardFace> { new() { PropertyPath = "#flag", Content = content } };

        var json = _mapper.ToJson(faces);
        json["#flag"]!["id"] = "mutated";

        Assert.That(content["id"]!.GetValue<string>(), Is.Not.EqualTo("mutated"));
    }
}

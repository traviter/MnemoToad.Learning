using MnemoToad.Learning.Data.Entities;
using MnemoToad.Learning.Data.Entities.Operations;
using NUnit.Framework;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace MnemoToad.Learning.Tests.Entities.Operations;

[TestFixture]
public class LeitnerCardFaceJsonMapperTests
{
    private readonly LeitnerCardFaceJsonMapper _mapper = new();

    [Test]
    public void ToJson_ReturnsOneKeyObjectKeyedByPropertyPath()
    {
        var face = new LeitnerCardFace { PropertyPath = "_canonicalName", Content = JsonValue.Create("France") };

        var json = _mapper.ToJson(face);

        Assert.That(json.Single().Key, Is.EqualTo("_canonicalName"));
        Assert.That(json["_canonicalName"]!.GetValue<string>(), Is.EqualTo("France"));
    }

    [Test]
    public void ToJson_ClonesContent_NotAliasedToEntity()
    {
        var content = new JsonObject { ["id"] = Guid.NewGuid().ToString() };
        var face = new LeitnerCardFace { PropertyPath = "#flag", Content = content };

        var json = _mapper.ToJson(face);
        json["#flag"]!["id"] = "mutated";

        Assert.That(content["id"]!.GetValue<string>(), Is.Not.EqualTo("mutated"));
    }

    [Test]
    public void UpdateFromJson_SetsContentFromPropertyPathKey()
    {
        var face = new LeitnerCardFace { PropertyPath = ".population" };
        var json = new JsonObject { [".population"] = 68000000 };

        _mapper.UpdateFromJson(face, json);

        Assert.That(face.Content!.GetValue<int>(), Is.EqualTo(68000000));
    }

    [Test]
    public void UpdateFromJson_WhenValueMissing_ThrowsValidationException()
    {
        var face = new LeitnerCardFace { PropertyPath = "_canonicalName" };
        var json = new JsonObject();

        Assert.Throws<ValidationException>(() => _mapper.UpdateFromJson(face, json));
    }

    [Test]
    public void UpdateFromJson_WhenValueExplicitlyNull_ThrowsValidationException()
    {
        var face = new LeitnerCardFace { PropertyPath = "_canonicalName" };
        var json = new JsonObject { ["_canonicalName"] = null };

        Assert.Throws<ValidationException>(() => _mapper.UpdateFromJson(face, json));
    }

    [Test]
    public void UpdateFromJson_ClonesFromJson_NotAliasedToSource()
    {
        var face = new LeitnerCardFace { PropertyPath = "#flag" };
        var value = new JsonObject { ["id"] = Guid.NewGuid().ToString() };
        var json = new JsonObject { ["#flag"] = value };

        _mapper.UpdateFromJson(face, json);
        value["id"] = "mutated";

        Assert.That(((JsonObject)face.Content!)["id"]!.GetValue<string>(), Is.Not.EqualTo("mutated"));
    }
}

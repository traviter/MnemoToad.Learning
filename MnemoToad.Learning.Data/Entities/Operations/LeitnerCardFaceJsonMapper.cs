using MnemoToad.Learning.Data.DbUtil;
using MnemoToad.Learning.Data.Entities;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace MnemoToad.Learning.Data.Entities.Operations;

public class LeitnerCardFaceJsonMapper : IEntityJsonMapper<LeitnerCardFace>
{
    public JsonObject ToJson(LeitnerCardFace entity) =>
        new JsonObject { [entity.PropertyPath] = entity.Content.DeepClone() };

    public void UpdateFromJson(LeitnerCardFace entity, JsonObject json) =>
        entity.Content = json[entity.PropertyPath]?.DeepClone()
            ?? throw new ValidationException($"The property '{entity.PropertyPath}' must have a value.");
}

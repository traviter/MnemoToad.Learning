using System.Text.Json.Nodes;

namespace MnemoToad.Learning.Data.DbUtil;

public interface IEntityJsonMapper<TEntity> : IJsonMapper<TEntity>
{
    void UpdateFromJson(TEntity entity, JsonObject json);
}

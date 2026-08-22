using System.Text.Json.Nodes;

namespace MnemoToad.Learning.Data.DbUtil;

public interface IJsonMapper<T>
{
    JsonObject ToJson(T item);
}

using System.Text.Json.Nodes;

namespace MnemoToad.Learning.Data.Entities;

public class LeitnerCardFace
{
    public Guid LeitnerCardId { get; set; }
    public string PropertyPath { get; set; } = string.Empty;
    public int FaceIndex { get; set; }
    public JsonNode Content { get; set; } = null!;
}

using MnemoToad.Learning.Data;
using MnemoToad.Learning.Data.Entities;
using System.Text.Json.Nodes;

namespace MnemoToad.Learning.Tests.TestSupport;

internal static class DbFixtures
{
    public static async Task<LeitnerDeck> CreateLeitnerDeckAsync(this IAppDbContext db, string? name = null, string? description = null)
    {
        var leitnerDeck = new LeitnerDeck { Name = name ?? $"LeitnerDeck_{Guid.NewGuid()}", Description = description };
        db.LeitnerDeck.Add(leitnerDeck);
        await db.SaveChangesAsync();
        return leitnerDeck;
    }

    public static async Task<LeitnerCard> CreateLeitnerCardAsync(this IAppDbContext db, Guid deckId, Guid? nodeId = null, JsonObject? properties = null)
    {
        var leitnerCard = new LeitnerCard { DeckId = deckId, NodeId = nodeId, BoxNumber = 0, DueUtc = DateTime.UtcNow };
        db.LeitnerCard.Add(leitnerCard);

        properties ??= new JsonObject { ["_canonicalName"] = $"LeitnerCard_{Guid.NewGuid()}" };
        var index = 0;
        foreach (var (path, content) in properties)
            db.LeitnerCardFace.Add(new LeitnerCardFace { LeitnerCardId = leitnerCard.Id, PropertyPath = path, FaceIndex = index++, Content = content! });

        await db.SaveChangesAsync();
        return leitnerCard;
    }
}

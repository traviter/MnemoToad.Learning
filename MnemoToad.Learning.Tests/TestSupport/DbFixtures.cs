using MnemoToad.Learning.Data;
using MnemoToad.Learning.Data.Entities;

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
}

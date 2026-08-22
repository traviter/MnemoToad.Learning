using Microsoft.EntityFrameworkCore;
using MnemoToad.Learning.Data.Entities;

namespace MnemoToad.Learning.Data.Repositories;

public class LeitnerCardFaceRepository : ILeitnerCardFaceRepository
{
    private readonly IAppDbContext _db;

    public LeitnerCardFaceRepository(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Dictionary<Guid, List<LeitnerCardFace>>> GetByCardIdsAsync(List<Guid> cardIds)
    {
        var faces = await _db.LeitnerCardFace
            .Where(f => cardIds.Contains(f.LeitnerCardId))
            .OrderBy(f => f.LeitnerCardId).ThenBy(f => f.FaceIndex)
            .ToListAsync();

        var facesByCard = faces.GroupBy(f => f.LeitnerCardId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var cardId in cardIds)
            facesByCard.TryAdd(cardId, new List<LeitnerCardFace>());
        return facesByCard;
    }

    public Task<Dictionary<Guid, List<LeitnerCardFace>>> GetByCardsAsync(List<LeitnerCard> cards) =>
        GetByCardIdsAsync(cards.Select(c => c.Id).ToList());
}

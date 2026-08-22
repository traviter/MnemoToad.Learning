using Microsoft.EntityFrameworkCore;
using MnemoToad.Learning.Data.DbUtil;
using MnemoToad.Learning.Data.Entities;

namespace MnemoToad.Learning.Data.Repositories;

public class LeitnerQuizRepository : ILeitnerQuizRepository
{
    private readonly IAppDbContext _db;
    private readonly IJsonMapper<IEnumerable<LeitnerCardFace>> _compositeFaceMapper;
    private readonly ILeitnerCardFaceRepository _faceRepository;

    public LeitnerQuizRepository(
        IAppDbContext db,
        IJsonMapper<IEnumerable<LeitnerCardFace>> compositeFaceMapper,
        ILeitnerCardFaceRepository faceRepository)
    {
        _db = db;
        _compositeFaceMapper = compositeFaceMapper;
        _faceRepository = faceRepository;
    }

    public async Task<List<LeitnerCard>> GetDueByDeckAsync(Guid deckId)
    {
        var now = DateTime.UtcNow;
        var cards = await _db.LeitnerCard
            .Where(c => c.DeckId == deckId && c.DueUtc != null && c.DueUtc <= now)
            .OrderBy(c => c.DueUtc)
            .ToListAsync();

        var facesByCard = await _faceRepository.GetByCardsAsync(cards);
        foreach (var card in cards)
            card.Properties = _compositeFaceMapper.ToJson(facesByCard[card.Id]);
        return cards;
    }
}

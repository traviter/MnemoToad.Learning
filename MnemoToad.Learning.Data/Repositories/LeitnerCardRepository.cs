using Microsoft.EntityFrameworkCore;
using MnemoToad.Learning.Data.DbUtil;
using MnemoToad.Learning.Data.Entities;
using System.Text.Json.Nodes;

namespace MnemoToad.Learning.Data.Repositories;

public class LeitnerCardRepository : ILeitnerCardRepository
{
    private readonly IAppDbContext _db;
    private readonly IJsonMapper<IEnumerable<LeitnerCardFace>> _compositeFaceMapper;
    private readonly ILeitnerCardFaceRepository _faceRepository;
    private readonly EntityCollectionSynchronizer<LeitnerCardFace, JsonNode?> _faceSync;

    public LeitnerCardRepository(
        IAppDbContext db,
        IEntityJsonMapper<LeitnerCardFace> faceMapper,
        IJsonMapper<IEnumerable<LeitnerCardFace>> compositeFaceMapper,
        ILeitnerCardFaceRepository faceRepository)
    {
        _db = db;
        _compositeFaceMapper = compositeFaceMapper;
        _faceRepository = faceRepository;

        _faceSync = new EntityCollectionSynchronizer<LeitnerCardFace, JsonNode?>(
            dbSet: _db.LeitnerCardFace,
            keySelector: f => f.PropertyPath,
            createBlank: (cardId, path) => new LeitnerCardFace { LeitnerCardId = cardId, PropertyPath = path },
            applyValue: (face, content) =>
                faceMapper.UpdateFromJson(face, new JsonObject { [face.PropertyPath] = content?.DeepClone() }));
    }

    public async Task<List<LeitnerCard>> GetByDeckAsync(Guid deckId)
    {
        var cards = await _db.LeitnerCard.Where(c => c.DeckId == deckId).ToListAsync();
        var facesByCard = await _faceRepository.GetByCardsAsync(cards);
        foreach (var card in cards)
            card.Properties = _compositeFaceMapper.ToJson(facesByCard[card.Id]);
        return cards;
    }

    public async Task<LeitnerCard?> GetByIdAsync(Guid id)
    {
        var card = await _db.LeitnerCard.FindAsync(id);
        if (card is null) return null;

        var facesByCard = await _faceRepository.GetByCardIdsAsync(new List<Guid> { id });
        card.Properties = _compositeFaceMapper.ToJson(facesByCard[id]);
        return card;
    }

    public async Task<List<LeitnerCard>> CreateManyAsync(List<LeitnerCard> cards)
    {
        foreach (var card in cards)
        {
            _db.LeitnerCard.Add(card);
            var desired = new Dictionary<string, JsonNode?>(card.Properties);
            var syncedFaces = _faceSync.Sync(card.Id, desired, new List<LeitnerCardFace>());
            for (var i = 0; i < syncedFaces.Count; i++)
                syncedFaces[i].FaceIndex = i;
        }

        await _db.SaveChangesAsync();
        return cards;
    }

    public async Task<bool> DeleteAsync(Guid id) =>
        await _db.ExecuteDeleteAsync(_db.LeitnerCard.Where(c => c.Id == id)) > 0;
}

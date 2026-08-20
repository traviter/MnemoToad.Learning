using Microsoft.EntityFrameworkCore;
using MnemoToad.Learning.Data.DbUtil;
using MnemoToad.Learning.Data.Entities;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace MnemoToad.Learning.Data.Repositories;

public class LeitnerCardRepository : ILeitnerCardRepository
{
    private readonly IAppDbContext _db;
    private readonly EntityCollectionSynchronizer<LeitnerCardFace, JsonNode?> _faceSync;

    public LeitnerCardRepository(IAppDbContext db)
    {
        _db = db;

        _faceSync = new EntityCollectionSynchronizer<LeitnerCardFace, JsonNode?>(
            dbSet: _db.LeitnerCardFace,
            keySelector: f => f.PropertyPath,
            createBlank: (cardId, path) => new LeitnerCardFace { LeitnerCardId = cardId, PropertyPath = path },
            applyValue: (face, content) => face.Content = content ?? throw new ValidationException($"The property '{face.PropertyPath}' must have a value."));
    }

    public async Task<List<LeitnerCard>> GetByDeckAsync(Guid deckId)
    {
        var cards = await _db.LeitnerCard.Where(c => c.DeckId == deckId).ToListAsync();
        var cardIds = cards.Select(c => c.Id).ToList();
        var faces = await _db.LeitnerCardFace
            .Where(f => cardIds.Contains(f.LeitnerCardId))
            .OrderBy(f => f.LeitnerCardId).ThenBy(f => f.FaceIndex)
            .ToListAsync();
        var facesByCard = faces.ToLookup(f => f.LeitnerCardId);

        foreach (var card in cards)
            card.Properties = ToProperties(facesByCard[card.Id]);

        return cards;
    }

    public async Task<LeitnerCard?> GetByIdAsync(Guid id)
    {
        var card = await _db.LeitnerCard.FindAsync(id);
        if (card is null) return null;

        var faces = await _db.LeitnerCardFace.Where(f => f.LeitnerCardId == id).OrderBy(f => f.FaceIndex).ToListAsync();
        card.Properties = ToProperties(faces);
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

    private static JsonObject ToProperties(IEnumerable<LeitnerCardFace> faces)
    {
        var properties = new JsonObject();
        foreach (var face in faces)
            properties[face.PropertyPath] = face.Content.DeepClone();
        return properties;
    }
}

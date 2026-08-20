using MnemoToad.Learning.Data.Entities;

namespace MnemoToad.Learning.Data.Repositories;

public interface ILeitnerCardRepository
{
    Task<List<LeitnerCard>> GetByDeckAsync(Guid deckId);
    Task<LeitnerCard?> GetByIdAsync(Guid id);
    Task<List<LeitnerCard>> CreateManyAsync(List<LeitnerCard> cards);
    Task<bool> DeleteAsync(Guid id);
}

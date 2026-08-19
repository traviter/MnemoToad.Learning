using MnemoToad.Learning.Data.Entities;

namespace MnemoToad.Learning.Data.Repositories;

public interface ILeitnerDeckRepository
{
    Task<List<LeitnerDeck>> GetAllAsync();
    Task<LeitnerDeck?> GetByIdAsync(Guid id);
    Task<LeitnerDeck> CreateAsync(LeitnerDeck leitnerDeck);
    Task<LeitnerDeck?> UpdateAsync(LeitnerDeck leitnerDeck);
    Task<bool> DeleteAsync(Guid id);
}

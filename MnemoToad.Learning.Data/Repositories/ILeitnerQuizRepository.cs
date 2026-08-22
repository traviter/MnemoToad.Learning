using MnemoToad.Learning.Data.Entities;

namespace MnemoToad.Learning.Data.Repositories;

public interface ILeitnerQuizRepository
{
    Task<List<LeitnerCard>> GetDueByDeckAsync(Guid deckId);
}

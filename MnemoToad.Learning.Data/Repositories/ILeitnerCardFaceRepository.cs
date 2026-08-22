using MnemoToad.Learning.Data.Entities;

namespace MnemoToad.Learning.Data.Repositories;

public interface ILeitnerCardFaceRepository
{
    Task<Dictionary<Guid, List<LeitnerCardFace>>> GetByCardIdsAsync(List<Guid> cardIds);
    Task<Dictionary<Guid, List<LeitnerCardFace>>> GetByCardsAsync(List<LeitnerCard> cards);
}

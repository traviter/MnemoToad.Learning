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

    public async Task<SubmitAnswersResult> SubmitAnswersAsync(IReadOnlyList<LeitnerAnswerSubmission> answers)
    {
        var schedulesByBox = await GetSchedulesByBoxAsync();
        var maxBox = schedulesByBox.Count == 0 ? 0 : schedulesByBox.Keys.Max();

        var cardsById = await GetCardsByIdAsync(answers.Select(a => a.CardId));
        if (answers.Any(a => !cardsById.ContainsKey(a.CardId))) return SubmitAnswersResult.CardNotFound;

        var now = DateTime.UtcNow;
        foreach (var answer in answers)
            ApplyAnswer(cardsById[answer.CardId], answer, schedulesByBox, maxBox, now);

        await _db.SaveChangesAsync();
        return SubmitAnswersResult.Success;
    }

    private async Task<Dictionary<int, LeitnerSchedule>> GetSchedulesByBoxAsync() =>
        (await _db.LeitnerSchedule.ToListAsync()).ToDictionary(s => s.BoxNumber);

    private async Task<Dictionary<Guid, LeitnerCard>> GetCardsByIdAsync(IEnumerable<Guid> cardIds)
    {
        var ids = cardIds.ToList();
        return await _db.LeitnerCard.Where(c => ids.Contains(c.Id)).ToDictionaryAsync(c => c.Id);
    }

    private static void ApplyAnswer(LeitnerCard card, LeitnerAnswerSubmission answer, Dictionary<int, LeitnerSchedule> schedulesByBox, int maxBox, DateTime now)
    {
        var newBox = ComputeNewBox(card.BoxNumber, answer, maxBox);
        card.BoxNumber = newBox;
        card.DueUtc = ComputeDueUtc(newBox, schedulesByBox, now);
        card.LastReviewedUtc = now;
    }

    private static int ComputeNewBox(int currentBox, LeitnerAnswerSubmission answer, int maxBox)
    {
        var box = answer.BoxNumber ?? (answer.Correct ? currentBox + 1 : 0);
        return Math.Min(box, maxBox);
    }

    private static DateTime ComputeDueUtc(int box, Dictionary<int, LeitnerSchedule> schedulesByBox, DateTime now)
    {
        if (!schedulesByBox.TryGetValue(box, out var schedule)) return now;
        var jitterHours = Random.Shared.Next(-schedule.VarianceHours, schedule.VarianceHours + 1);
        return now.AddHours(schedule.IntervalHours + jitterHours);
    }
}

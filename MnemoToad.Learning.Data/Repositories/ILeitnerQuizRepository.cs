using MnemoToad.Learning.Data.Entities;

namespace MnemoToad.Learning.Data.Repositories;

public interface ILeitnerQuizRepository
{
    Task<List<LeitnerCard>> GetDueByDeckAsync(Guid deckId);
    Task<SubmitAnswersResult> SubmitAnswersAsync(IReadOnlyList<LeitnerAnswerSubmission> answers);
}

/// <summary>A single card's answer, to be applied by <see cref="ILeitnerQuizRepository.SubmitAnswersAsync"/>.</summary>
public record LeitnerAnswerSubmission(Guid CardId, bool Correct, int? BoxNumber);

/// <summary>The outcome of <see cref="ILeitnerQuizRepository.SubmitAnswersAsync"/>.</summary>
public enum SubmitAnswersResult
{
    Success,
    CardNotFound
}

using System.ComponentModel.DataAnnotations;

namespace MnemoToad.Learning.Api.Contracts;

/// <summary>A single card's answer, submitted as part of a batch to the quiz answers endpoint.</summary>
/// <param name="CardId">The id of the LeitnerCard being answered. Required.</param>
/// <param name="Correct">Whether the answer was correct. Required.</param>
/// <param name="BoxNumber">
/// An explicit Leitner box to set the card to, overriding the default box computation.
/// Optional — when omitted, the resulting box is computed from <c>Correct</c>: the card's
/// current box plus one if correct, or reset to zero if incorrect. When provided, this
/// value is used as-is regardless of <c>Correct</c>. Must be nonnegative. Either way, if the
/// resulting box exceeds the highest configured Leitner schedule box, it's clamped down to
/// that box rather than rejected.
/// </param>
public record LeitnerAnswerRequest(
    [Required] Guid? CardId,
    [Required] bool? Correct,
    [Range(0, int.MaxValue)] int? BoxNumber);

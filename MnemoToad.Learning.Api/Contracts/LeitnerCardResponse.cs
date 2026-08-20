namespace MnemoToad.Learning.Api.Contracts;

/// <summary>A LeitnerCard.</summary>
/// <param name="Id">The card's id.</param>
/// <param name="DeckId">The id of the LeitnerDeck this card belongs to.</param>
/// <param name="NodeId">
/// The id of the Knowledge node this card is pinned to, or null for a manually-authored card
/// with no Knowledge link.
/// </param>
/// <param name="BoxNumber">The Leitner box the card is currently in. New cards start at 0.</param>
/// <param name="DueUtc">When the card is next due for review. New cards are immediately due.</param>
/// <param name="LastReviewedUtc">When the card was last reviewed, or null if never reviewed.</param>
/// <param name="Properties">
/// Key/value pairs keyed by Knowledge property-path string (see architecture docs §3 Path DSL).
/// </param>
public record LeitnerCardResponse(
    Guid Id,
    Guid DeckId,
    Guid? NodeId,
    int BoxNumber,
    DateTime DueUtc,
    DateTime? LastReviewedUtc,
    Dictionary<string, object?> Properties);

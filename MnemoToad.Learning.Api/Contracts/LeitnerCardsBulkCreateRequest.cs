using System.ComponentModel.DataAnnotations;

namespace MnemoToad.Learning.Api.Contracts;

/// <summary>The body for bulk-creating LeitnerCards into a single LeitnerDeck.</summary>
/// <param name="DeckId">The id of the LeitnerDeck the new cards belong to. Required.</param>
/// <param name="Cards">The cards to create. Must contain at least one card.</param>
public record LeitnerCardsBulkCreateRequest(
    [Required] Guid? DeckId,
    [Required] IReadOnlyList<LeitnerCardCreateRequest> Cards);

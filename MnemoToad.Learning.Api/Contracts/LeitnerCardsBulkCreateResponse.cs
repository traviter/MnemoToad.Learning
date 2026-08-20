namespace MnemoToad.Learning.Api.Contracts;

/// <summary>The created LeitnerCards from a bulk-create request.</summary>
/// <param name="Cards">The newly created cards, in the same order as the request.</param>
public record LeitnerCardsBulkCreateResponse(IReadOnlyList<LeitnerCardResponse> Cards);

namespace MnemoToad.Learning.Api.Contracts;

/// <summary>A LeitnerDeck.</summary>
/// <param name="Id">The deck's id.</param>
/// <param name="Name">The deck's display name. Unique.</param>
/// <param name="Description">Free-text description of what this deck covers.</param>
public record LeitnerDeckResponse(Guid Id, string Name, string? Description);

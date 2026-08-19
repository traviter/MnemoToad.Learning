namespace MnemoToad.Learning.Data.Entities;

/// <summary>A named collection of flashcards studied together.</summary>
public class LeitnerDeck
{
    /// <summary>The deck's id.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The deck's display name. Not unique — two decks may share a name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Free-text description of what this deck covers.</summary>
    public string? Description { get; set; }
}

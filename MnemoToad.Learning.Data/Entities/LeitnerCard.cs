using System.Text.Json.Nodes;

namespace MnemoToad.Learning.Data.Entities;

/// <summary>A single flashcard within a LeitnerDeck.</summary>
public class LeitnerCard
{
    /// <summary>The card's id.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The id of the LeitnerDeck this card belongs to.</summary>
    public Guid DeckId { get; set; }

    /// <summary>The id of the Knowledge node this card is pinned to, or null if manually authored.</summary>
    public Guid? NodeId { get; set; }

    /// <summary>The Leitner box the card is currently in. New cards start at 0.</summary>
    public int BoxNumber { get; set; }

    /// <summary>When the card is next due for review, or null if it isn't scheduled.</summary>
    public DateTime? DueUtc { get; set; }

    /// <summary>When the card was last reviewed, or null if never reviewed.</summary>
    public DateTime? LastReviewedUtc { get; set; }

    /// <summary>Key/value pairs keyed by Knowledge property-path string, in entry order. Not a mapped column.</summary>
    public JsonObject Properties { get; set; } = new();
}

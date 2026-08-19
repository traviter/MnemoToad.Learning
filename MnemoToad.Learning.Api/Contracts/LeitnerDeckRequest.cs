using System.ComponentModel.DataAnnotations;

namespace MnemoToad.Learning.Api.Contracts;

/// <summary>The body for creating or replacing a LeitnerDeck.</summary>
/// <param name="Name">The deck's display name. Must be unique. Required.</param>
/// <param name="Description">Free-text description of what this deck covers.</param>
public record LeitnerDeckRequest([Required] string Name, string? Description);

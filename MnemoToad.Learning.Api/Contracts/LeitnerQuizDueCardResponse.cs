using System.Text.Json.Nodes;

namespace MnemoToad.Learning.Api.Contracts;

/// <summary>A single due card returned from the quiz due-cards endpoint.</summary>
/// <param name="Id">The card's id.</param>
/// <param name="BoxNumber">The card's current Leitner box.</param>
/// <param name="Properties">
/// The card's face content, in presentation order (same order-preservation invariant as
/// <see cref="LeitnerCardCreateRequest"/>).
/// </param>
public record LeitnerQuizDueCardResponse(Guid Id, int BoxNumber, JsonObject Properties);

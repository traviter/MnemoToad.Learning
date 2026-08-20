using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace MnemoToad.Learning.Api.Contracts;

/// <summary>One card within a <see cref="LeitnerCardsBulkCreateRequest"/>.</summary>
/// <param name="NodeId">
/// The id of the Knowledge node this card is pinned to, if any. Optional — omit for a
/// manually-authored card with no Knowledge link.
/// </param>
/// <param name="Properties">
/// Key/value pairs keyed by Knowledge property-path string (see architecture docs §3 Path DSL:
/// <c>_name</c> for a node column, <c>.name</c> for an attribute, <c>#name</c> for media). Each
/// value is exactly what that path resolves to in Knowledge, so a card's properties can be copied
/// straight out of a Knowledge lookup with no reshaping. Must contain at least one entry, in the
/// order they should be presented — that order is preserved through storage and every response.
/// </param>
public record LeitnerCardCreateRequest(Guid? NodeId, [Required, MinLength(1)] JsonObject Properties);

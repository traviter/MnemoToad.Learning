using Microsoft.AspNetCore.Mvc;
using MnemoToad.Learning.Api.Contracts;
using MnemoToad.Learning.Data.Entities;
using MnemoToad.Learning.Data.Repositories;
using System.ComponentModel.DataAnnotations;

namespace MnemoToad.Learning.Api.Controllers;

/// <summary>
/// A LeitnerCard is a single flashcard within a LeitnerDeck. Its content lives entirely in a
/// <c>properties</c> bag keyed by Knowledge property-path string — there is no separate
/// card-face resource.
/// </summary>
[ApiController]
[Route("leitner/cards")]
public class LeitnerCardsController : ControllerBase
{
    private readonly ILeitnerCardRepository _cardRepository;
    private readonly ILeitnerDeckRepository _deckRepository;

    public LeitnerCardsController(ILeitnerCardRepository cardRepository, ILeitnerDeckRepository deckRepository)
    {
        _cardRepository = cardRepository;
        _deckRepository = deckRepository;
    }

    /// <summary>Bulk-creates LeitnerCards into a single LeitnerDeck.</summary>
    /// <remarks>Each card's <c>NodeId</c> is optional — omit it for a manually-authored card with no Knowledge link.</remarks>
    /// <param name="request">The target deck and the cards to create.</param>
    /// <response code="201">The created LeitnerCards.</response>
    /// <response code="400">
    /// <c>DeckId</c> was missing, <c>Cards</c> was empty, or a card had no properties.
    /// </response>
    /// <response code="404">No LeitnerDeck exists with the given <c>DeckId</c>.</response>
    [HttpPost]
    [ProducesResponseType(typeof(LeitnerCardsBulkCreateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(LeitnerCardsBulkCreateRequest request)
    {
        if (await _deckRepository.GetByIdAsync(request.DeckId!.Value) is null) return NotFound();

        var cards = request.Cards.Select(c => new LeitnerCard
        {
            DeckId = request.DeckId.Value,
            NodeId = c.NodeId,
            Properties = c.Properties,
            BoxNumber = 0,
            DueUtc = DateTime.UtcNow,
            LastReviewedUtc = null
        }).ToList();

        var created = await _cardRepository.CreateManyAsync(cards);
        return StatusCode(StatusCodes.Status201Created, new LeitnerCardsBulkCreateResponse(created));
    }

    /// <summary>Lists every LeitnerCard in a deck.</summary>
    /// <param name="deckId">The id of the LeitnerDeck to list cards for. Required.</param>
    /// <response code="200">All LeitnerCards in the deck, in no particular order.</response>
    /// <response code="400"><c>deckId</c> was missing or not a valid GUID.</response>
    /// <response code="404">No LeitnerDeck exists with that id.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<LeitnerCard>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByDeck([FromQuery, Required] Guid? deckId)
    {
        var cards = await _cardRepository.GetByDeckAsync(deckId!.Value);
        if (cards.Count == 0 && await _deckRepository.GetByIdAsync(deckId.Value) is null) return NotFound();
        return Ok(cards);
    }

    /// <summary>Gets a single LeitnerCard by id.</summary>
    /// <param name="id">The card's id.</param>
    /// <response code="200">The matching LeitnerCard.</response>
    /// <response code="404">No LeitnerCard exists with that id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LeitnerCard), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id) =>
        await _cardRepository.GetByIdAsync(id) is { } card ? Ok(card) : NotFound();

    /// <summary>Deletes a single LeitnerCard. Cascades to any associated property data.</summary>
    /// <param name="id">The card's id.</param>
    /// <response code="204">The LeitnerCard was deleted.</response>
    /// <response code="404">No LeitnerCard exists with that id.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id) =>
        await _cardRepository.DeleteAsync(id) ? NoContent() : NotFound();
}

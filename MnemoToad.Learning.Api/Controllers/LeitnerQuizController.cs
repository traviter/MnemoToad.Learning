using Microsoft.AspNetCore.Mvc;
using MnemoToad.Learning.Api.Contracts;
using MnemoToad.Learning.Data.Repositories;
using System.ComponentModel.DataAnnotations;

namespace MnemoToad.Learning.Api.Controllers;

/// <summary>
/// Quiz-taking endpoints — the study-session view over LeitnerCards, as opposed to the
/// CRUD/authoring endpoints on <see cref="LeitnerCardsController"/>.
/// </summary>
[ApiController]
[Route("leitner/quiz")]
public class LeitnerQuizController : ControllerBase
{
    private readonly ILeitnerQuizRepository _quizRepository;
    private readonly ILeitnerDeckRepository _deckRepository;

    public LeitnerQuizController(ILeitnerQuizRepository quizRepository, ILeitnerDeckRepository deckRepository)
    {
        _quizRepository = quizRepository;
        _deckRepository = deckRepository;
    }

    /// <summary>Gets the cards in a deck that are due for review, earliest-due first.</summary>
    /// <param name="deckId">The id of the LeitnerDeck to get due cards for. Required.</param>
    /// <response code="200">
    /// The due LeitnerCards in the deck (<c>DueUtc</c> at or before now), ordered by
    /// <c>DueUtc</c> ascending. Empty if the deck exists but nothing is due.
    /// </response>
    /// <response code="400"><c>deckId</c> was missing or not a valid GUID.</response>
    /// <response code="404">No LeitnerDeck exists with that id.</response>
    [HttpGet("due-cards")]
    [ProducesResponseType(typeof(IEnumerable<LeitnerQuizDueCardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDueCards([FromQuery, Required] Guid? deckId)
    {
        var cards = await _quizRepository.GetDueByDeckAsync(deckId!.Value);
        if (cards.Count == 0 && await _deckRepository.GetByIdAsync(deckId.Value) is null) return NotFound();
        return Ok(cards.Select(c => new LeitnerQuizDueCardResponse(c.Id, c.BoxNumber, c.Properties)));
    }
}

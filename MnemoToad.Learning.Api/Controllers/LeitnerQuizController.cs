using Microsoft.AspNetCore.Mvc;
using MnemoToad.Learning.Api.Contracts;
using MnemoToad.Learning.Data.Repositories;
using System.ComponentModel.DataAnnotations;

namespace MnemoToad.Learning.Api.Controllers;

/// <summary>
/// Quiz-taking endpoints — reading due cards and submitting answers for them, as opposed
/// to the CRUD/authoring endpoints on <see cref="LeitnerCardsController"/>.
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

    /// <summary>Submits one or more answers, updating each card's Leitner box and rescheduling it.</summary>
    /// <remarks>
    /// For each answer: if <c>BoxNumber</c> is omitted, the card's box becomes its current
    /// box plus one when <c>Correct</c> is true, or zero when <c>Correct</c> is false. If
    /// <c>BoxNumber</c> is provided, it's used as-is instead. Either way, the card's due
    /// date is recomputed from the Leitner schedule for the resulting box, and its
    /// last-correct timestamp is updated whenever <c>Correct</c> is true.
    /// </remarks>
    /// <param name="answers">The answers to apply. Must contain at least one entry.</param>
    /// <response code="204">Every answer was applied.</response>
    /// <response code="400">
    /// <c>answers</c> was empty, or an entry was missing <c>CardId</c>/<c>Correct</c>, or an
    /// explicit <c>BoxNumber</c> was outside the configured schedule's range.
    /// </response>
    /// <response code="404">One or more <c>CardId</c> doesn't match an existing LeitnerCard.</response>
    [HttpPost("cards/answers")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public Task<IActionResult> SubmitAnswers([Required, MinLength(1)] IReadOnlyList<LeitnerAnswerRequest> answers) =>
        Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status501NotImplemented));
}

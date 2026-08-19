using Microsoft.AspNetCore.Mvc;
using MnemoToad.Learning.Api.Contracts;

namespace MnemoToad.Learning.Api.Controllers;

/// <summary>
/// A LeitnerDeck is a named collection of flashcards studied together — it has no fields of its
/// own beyond a name and description.
/// </summary>
[ApiController]
[Route("leitner/decks")]
public class LeitnerDecksController : ControllerBase
{
    /// <summary>Lists every LeitnerDeck.</summary>
    /// <response code="200">All LeitnerDecks, in no particular order.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<LeitnerDeckResponse>), StatusCodes.Status200OK)]
    public IActionResult GetAll() =>
        StatusCode(StatusCodes.Status501NotImplemented);

    /// <summary>Gets a single LeitnerDeck by id.</summary>
    /// <param name="id">The deck's id.</param>
    /// <response code="200">The matching LeitnerDeck.</response>
    /// <response code="404">No LeitnerDeck exists with that id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LeitnerDeckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(Guid id) =>
        StatusCode(StatusCodes.Status501NotImplemented);

    /// <summary>Creates a new LeitnerDeck.</summary>
    /// <param name="request">The name and optional description for the new deck.</param>
    /// <response code="201">The created LeitnerDeck.</response>
    /// <response code="400"><c>Name</c> was missing.</response>
    [HttpPost]
    [ProducesResponseType(typeof(LeitnerDeckResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Create(LeitnerDeckRequest request) =>
        StatusCode(StatusCodes.Status501NotImplemented);

    /// <summary>Replaces an existing LeitnerDeck's name and description.</summary>
    /// <param name="id">The deck's id.</param>
    /// <param name="request">The deck's new name and optional description.</param>
    /// <response code="200">The updated LeitnerDeck.</response>
    /// <response code="400"><c>Name</c> was missing.</response>
    /// <response code="404">No LeitnerDeck exists with that id.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LeitnerDeckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Update(Guid id, LeitnerDeckRequest request) =>
        StatusCode(StatusCodes.Status501NotImplemented);

    /// <summary>Deletes a LeitnerDeck.</summary>
    /// <param name="id">The deck's id.</param>
    /// <response code="204">The LeitnerDeck was deleted.</response>
    /// <response code="404">No LeitnerDeck exists with that id.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(Guid id) =>
        StatusCode(StatusCodes.Status501NotImplemented);
}

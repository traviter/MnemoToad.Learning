using Microsoft.AspNetCore.Mvc;
using MnemoToad.Learning.Api.Contracts;
using MnemoToad.Learning.Data.Entities;
using MnemoToad.Learning.Data.Repositories;

namespace MnemoToad.Learning.Api.Controllers;

/// <summary>
/// A LeitnerDeck is a named collection of flashcards studied together — it has no fields of its
/// own beyond a name and description.
/// </summary>
[ApiController]
[Route("leitner/decks")]
public class LeitnerDecksController : ControllerBase
{
    private readonly ILeitnerDeckRepository _repository;

    public LeitnerDecksController(ILeitnerDeckRepository repository)
    {
        _repository = repository;
    }

    /// <summary>Lists every LeitnerDeck.</summary>
    /// <response code="200">All LeitnerDecks, in no particular order.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<LeitnerDeck>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() =>
        Ok(await _repository.GetAllAsync());

    /// <summary>Gets a single LeitnerDeck by id.</summary>
    /// <param name="id">The deck's id.</param>
    /// <response code="200">The matching LeitnerDeck.</response>
    /// <response code="404">No LeitnerDeck exists with that id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LeitnerDeck), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id) =>
        await _repository.GetByIdAsync(id) is { } leitnerDeck ? Ok(leitnerDeck) : NotFound();

    /// <summary>Creates a new LeitnerDeck.</summary>
    /// <param name="request">The name and optional description for the new deck.</param>
    /// <response code="201">The created LeitnerDeck.</response>
    /// <response code="400"><c>Name</c> was missing.</response>
    [HttpPost]
    [ProducesResponseType(typeof(LeitnerDeck), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(LeitnerDeckRequest request)
    {
        var created = await _repository.CreateAsync(new LeitnerDeck { Name = request.Name, Description = request.Description });
        return Created($"/leitner/decks/{created.Id}", created);
    }

    /// <summary>Replaces an existing LeitnerDeck's name and description.</summary>
    /// <param name="id">The deck's id.</param>
    /// <param name="request">The deck's new name and optional description.</param>
    /// <response code="200">The updated LeitnerDeck.</response>
    /// <response code="400"><c>Name</c> was missing.</response>
    /// <response code="404">No LeitnerDeck exists with that id.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LeitnerDeck), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, LeitnerDeckRequest request)
    {
        var updated = await _repository.UpdateAsync(new LeitnerDeck { Id = id, Name = request.Name, Description = request.Description });
        return updated is not null ? Ok(updated) : NotFound();
    }

    /// <summary>Deletes a LeitnerDeck. Also deletes every LeitnerCard in the deck.</summary>
    /// <param name="id">The deck's id.</param>
    /// <response code="204">The LeitnerDeck, and all LeitnerCards in it, were deleted.</response>
    /// <response code="404">No LeitnerDeck exists with that id.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id) =>
        await _repository.DeleteAsync(id) ? NoContent() : NotFound();
}

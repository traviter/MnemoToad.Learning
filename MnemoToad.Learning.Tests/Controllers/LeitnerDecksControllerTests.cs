using Microsoft.AspNetCore.Mvc;
using Moq;
using MnemoToad.Learning.Api.Contracts;
using MnemoToad.Learning.Api.Controllers;
using MnemoToad.Learning.Data.Entities;
using MnemoToad.Learning.Data.Repositories;
using NUnit.Framework;

namespace MnemoToad.Learning.Tests.Controllers;

[TestFixture]
public class LeitnerDecksControllerTests
{
    private Mock<ILeitnerDeckRepository> _repository = null!;
    private LeitnerDecksController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<ILeitnerDeckRepository>();
        _controller = new LeitnerDecksController(_repository.Object);
    }

    [Test]
    public async Task GetAll_ReturnsOkWithLeitnerDecks()
    {
        var leitnerDecks = new List<LeitnerDeck> { new() { Id = Guid.NewGuid(), Name = "World Capitals" } };
        _repository.Setup(r => r.GetAllAsync()).ReturnsAsync(leitnerDecks);

        var result = await _controller.GetAll();

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.SameAs(leitnerDecks));
    }

    [Test]
    public async Task GetById_WhenExists_ReturnsOkWithLeitnerDeck()
    {
        var leitnerDeck = new LeitnerDeck { Id = Guid.NewGuid(), Name = "World Capitals" };
        _repository.Setup(r => r.GetByIdAsync(leitnerDeck.Id)).ReturnsAsync(leitnerDeck);

        var result = await _controller.GetById(leitnerDeck.Id);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.SameAs(leitnerDeck));
    }

    [Test]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((LeitnerDeck?)null);

        var result = await _controller.GetById(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Create_WithValidRequest_ReturnsCreatedWithLeitnerDeck()
    {
        var created = new LeitnerDeck { Id = Guid.NewGuid(), Name = "World Capitals", Description = "Country name, capital city" };
        _repository.Setup(r => r.CreateAsync(It.Is<LeitnerDeck>(d => d.Name == "World Capitals" && d.Description == "Country name, capital city")))
            .ReturnsAsync(created);

        var result = await _controller.Create(new LeitnerDeckRequest("World Capitals", "Country name, capital city"));

        var createdResult = result as CreatedResult;
        Assert.That(createdResult, Is.Not.Null);
        Assert.That(createdResult!.Location, Is.EqualTo($"/leitner/decks/{created.Id}"));
        Assert.That(createdResult.Value, Is.SameAs(created));
    }

    [Test]
    public async Task Update_WhenExists_ReturnsOkWithUpdatedLeitnerDeck()
    {
        var id = Guid.NewGuid();
        var updated = new LeitnerDeck { Id = id, Name = "World Capitals", Description = "New description" };
        _repository.Setup(r => r.UpdateAsync(It.Is<LeitnerDeck>(d => d.Id == id))).ReturnsAsync(updated);

        var result = await _controller.Update(id, new LeitnerDeckRequest("World Capitals", "New description"));

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.SameAs(updated));
    }

    [Test]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        _repository.Setup(r => r.UpdateAsync(It.IsAny<LeitnerDeck>())).ReturnsAsync((LeitnerDeck?)null);

        var result = await _controller.Update(Guid.NewGuid(), new LeitnerDeckRequest("World Capitals", null));

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Delete_WhenExists_ReturnsNoContent()
    {
        _repository.Setup(r => r.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        var result = await _controller.Delete(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        _repository.Setup(r => r.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var result = await _controller.Delete(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }
}

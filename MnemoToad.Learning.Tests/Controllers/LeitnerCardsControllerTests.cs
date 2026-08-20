using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MnemoToad.Learning.Api.Contracts;
using MnemoToad.Learning.Api.Controllers;
using MnemoToad.Learning.Data.Entities;
using MnemoToad.Learning.Data.Repositories;
using NUnit.Framework;
using System.Text.Json.Nodes;

namespace MnemoToad.Learning.Tests.Controllers;

[TestFixture]
public class LeitnerCardsControllerTests
{
    private Mock<ILeitnerCardRepository> _cardRepository = null!;
    private Mock<ILeitnerDeckRepository> _deckRepository = null!;
    private LeitnerCardsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _cardRepository = new Mock<ILeitnerCardRepository>();
        _deckRepository = new Mock<ILeitnerDeckRepository>();
        _controller = new LeitnerCardsController(_cardRepository.Object, _deckRepository.Object);
    }

    [Test]
    public async Task Create_WhenDeckDoesNotExist_ReturnsNotFoundAndNeverCreatesCards()
    {
        _deckRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((LeitnerDeck?)null);
        var request = new LeitnerCardsBulkCreateRequest(Guid.NewGuid(), new List<LeitnerCardCreateRequest>
        {
            new(NodeId: null, Properties: new JsonObject { ["_canonicalName"] = "France" })
        });

        var result = await _controller.Create(request);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
        _cardRepository.Verify(r => r.CreateManyAsync(It.IsAny<List<LeitnerCard>>()), Times.Never);
    }

    [Test]
    public async Task Create_WhenDeckExists_ReturnsCreatedWithCards()
    {
        var deckId = Guid.NewGuid();
        _deckRepository.Setup(r => r.GetByIdAsync(deckId)).ReturnsAsync(new LeitnerDeck { Id = deckId, Name = "World Capitals" });
        var createdCards = new List<LeitnerCard> { new() { DeckId = deckId } };
        _cardRepository.Setup(r => r.CreateManyAsync(It.IsAny<List<LeitnerCard>>())).ReturnsAsync(createdCards);
        var request = new LeitnerCardsBulkCreateRequest(deckId, new List<LeitnerCardCreateRequest>
        {
            new(NodeId: null, Properties: new JsonObject { ["_canonicalName"] = "France" })
        });

        var result = await _controller.Create(request);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult, Is.Not.Null);
        Assert.That(statusResult!.StatusCode, Is.EqualTo(StatusCodes.Status201Created));
        var response = statusResult.Value as LeitnerCardsBulkCreateResponse;
        Assert.That(response!.Cards, Is.SameAs(createdCards));
    }

    [Test]
    public async Task GetByDeck_WhenCardsFound_ReturnsOkWithoutCheckingDeckExists()
    {
        var deckId = Guid.NewGuid();
        var cards = new List<LeitnerCard> { new() { DeckId = deckId } };
        _cardRepository.Setup(r => r.GetByDeckAsync(deckId)).ReturnsAsync(cards);

        var result = await _controller.GetByDeck(deckId);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.SameAs(cards));
        _deckRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task GetByDeck_WhenNoCardsAndDeckExists_ReturnsOkWithEmptyList()
    {
        var deckId = Guid.NewGuid();
        _cardRepository.Setup(r => r.GetByDeckAsync(deckId)).ReturnsAsync(new List<LeitnerCard>());
        _deckRepository.Setup(r => r.GetByIdAsync(deckId)).ReturnsAsync(new LeitnerDeck { Id = deckId, Name = "World Capitals" });

        var result = await _controller.GetByDeck(deckId);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.InstanceOf<List<LeitnerCard>>().And.Empty);
    }

    [Test]
    public async Task GetByDeck_WhenNoCardsAndDeckMissing_ReturnsNotFound()
    {
        var deckId = Guid.NewGuid();
        _cardRepository.Setup(r => r.GetByDeckAsync(deckId)).ReturnsAsync(new List<LeitnerCard>());
        _deckRepository.Setup(r => r.GetByIdAsync(deckId)).ReturnsAsync((LeitnerDeck?)null);

        var result = await _controller.GetByDeck(deckId);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetById_WhenExists_ReturnsOkWithCard()
    {
        var card = new LeitnerCard { Id = Guid.NewGuid() };
        _cardRepository.Setup(r => r.GetByIdAsync(card.Id)).ReturnsAsync(card);

        var result = await _controller.GetById(card.Id);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.SameAs(card));
    }

    [Test]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _cardRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((LeitnerCard?)null);

        var result = await _controller.GetById(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Delete_WhenExists_ReturnsNoContent()
    {
        _cardRepository.Setup(r => r.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        var result = await _controller.Delete(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        _cardRepository.Setup(r => r.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var result = await _controller.Delete(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }
}

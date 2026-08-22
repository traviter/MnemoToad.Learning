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
public class LeitnerQuizControllerTests
{
    private Mock<ILeitnerQuizRepository> _quizRepository = null!;
    private Mock<ILeitnerDeckRepository> _deckRepository = null!;
    private LeitnerQuizController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _quizRepository = new Mock<ILeitnerQuizRepository>();
        _deckRepository = new Mock<ILeitnerDeckRepository>();
        _controller = new LeitnerQuizController(_quizRepository.Object, _deckRepository.Object);
    }

    [Test]
    public async Task GetDueCards_WhenCardsFound_ReturnsOkWithoutCheckingDeckExists()
    {
        var deckId = Guid.NewGuid();
        var cards = new List<LeitnerCard> { new() { Id = Guid.NewGuid(), DeckId = deckId, BoxNumber = 1, Properties = new JsonObject { ["_canonicalName"] = "France" } } };
        _quizRepository.Setup(r => r.GetDueByDeckAsync(deckId)).ReturnsAsync(cards);

        var result = await _controller.GetDueCards(deckId);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var response = ok!.Value as IEnumerable<LeitnerQuizDueCardResponse>;
        Assert.That(response!.Single().Id, Is.EqualTo(cards[0].Id));
        _deckRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task GetDueCards_MapsCardsToResponseShape()
    {
        var deckId = Guid.NewGuid();
        var properties = new JsonObject { ["_canonicalName"] = "France" };
        var cards = new List<LeitnerCard> { new() { Id = Guid.NewGuid(), DeckId = deckId, BoxNumber = 2, Properties = properties } };
        _quizRepository.Setup(r => r.GetDueByDeckAsync(deckId)).ReturnsAsync(cards);

        var result = await _controller.GetDueCards(deckId);

        var ok = result as OkObjectResult;
        var response = (ok!.Value as IEnumerable<LeitnerQuizDueCardResponse>)!.Single();
        Assert.That(response.Id, Is.EqualTo(cards[0].Id));
        Assert.That(response.BoxNumber, Is.EqualTo(2));
        Assert.That(response.Properties, Is.SameAs(properties));
    }

    [Test]
    public async Task GetDueCards_WhenNoCardsAndDeckExists_ReturnsOkWithEmptyList()
    {
        var deckId = Guid.NewGuid();
        _quizRepository.Setup(r => r.GetDueByDeckAsync(deckId)).ReturnsAsync(new List<LeitnerCard>());
        _deckRepository.Setup(r => r.GetByIdAsync(deckId)).ReturnsAsync(new LeitnerDeck { Id = deckId, Name = "World Capitals" });

        var result = await _controller.GetDueCards(deckId);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.InstanceOf<IEnumerable<LeitnerQuizDueCardResponse>>().And.Empty);
    }

    [Test]
    public async Task GetDueCards_WhenNoCardsAndDeckMissing_ReturnsNotFound()
    {
        var deckId = Guid.NewGuid();
        _quizRepository.Setup(r => r.GetDueByDeckAsync(deckId)).ReturnsAsync(new List<LeitnerCard>());
        _deckRepository.Setup(r => r.GetByIdAsync(deckId)).ReturnsAsync((LeitnerDeck?)null);

        var result = await _controller.GetDueCards(deckId);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }
}

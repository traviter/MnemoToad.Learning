using Microsoft.AspNetCore.Mvc;
using MnemoToad.Learning.Api.Contracts;
using MnemoToad.Learning.Data.Entities;
using MnemoToad.Learning.Tests.TestSupport;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MnemoToad.Learning.Tests.SystemTests;

// Real HTTP requests through the full app pipeline. LeitnerDeck/LeitnerCard setup seeds straight
// into the in-memory DB via DbFixtures (same "setup-only preconditions skip HTTP" rule
// LeitnerCardsControllerSystemTests already follows) -- this is also the only way to give a
// LeitnerCard an arbitrary DueUtc/BoxNumber, since the real Create endpoint always stamps "now".
[TestFixture]
public class LeitnerQuizControllerSystemTests
{
    private MockedDbWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new MockedDbWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task GetDueCards_WhenDeckDoesNotExist_Returns404()
    {
        var response = await _client.GetAsync($"/leitner/quiz/due-cards?deckId={Guid.NewGuid()}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetDueCards_WithMissingDeckId_Returns400WithValidationErrors()
    {
        var response = await _client.GetAsync("/leitner/quiz/due-cards");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.That(problem!.Errors, Contains.Key("deckId"));
    }

    [Test]
    public async Task GetDueCards_WithInvalidDeckIdFormat_Returns400WithValidationErrors()
    {
        var response = await _client.GetAsync("/leitner/quiz/due-cards?deckId=not-a-guid");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.That(problem!.Errors, Contains.Key("deckId"));
    }

    [Test]
    public async Task GetDueCards_WhenDeckHasNoCards_ReturnsOkWithEmptyList()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();

        var response = await _client.GetAsync($"/leitner/quiz/due-cards?deckId={deck.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var cards = await response.Content.ReadFromJsonAsync<List<LeitnerQuizDueCardResponse>>();
        Assert.That(cards, Is.Empty);
    }

    [Test]
    public async Task GetDueCards_ReturnsOnlyCardsInThatDeck()
    {
        var deck1 = await _factory.Db.CreateLeitnerDeckAsync();
        var deck2 = await _factory.Db.CreateLeitnerDeckAsync();
        var card1 = await _factory.Db.CreateLeitnerCardAsync(deck1.Id, dueUtc: DateTime.UtcNow.AddMinutes(-5));
        await _factory.Db.CreateLeitnerCardAsync(deck2.Id, dueUtc: DateTime.UtcNow.AddMinutes(-5));

        var response = await _client.GetAsync($"/leitner/quiz/due-cards?deckId={deck1.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var cards = await response.Content.ReadFromJsonAsync<List<LeitnerQuizDueCardResponse>>();
        Assert.That(cards!.Select(c => c.Id), Is.EquivalentTo(new[] { card1.Id }));
    }

    [Test]
    public async Task GetDueCards_ExcludesCardsNotYetDue()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();
        var dueCard = await _factory.Db.CreateLeitnerCardAsync(deck.Id, dueUtc: DateTime.UtcNow.AddMinutes(-1));
        await _factory.Db.CreateLeitnerCardAsync(deck.Id, dueUtc: DateTime.UtcNow.AddMinutes(10));

        var response = await _client.GetAsync($"/leitner/quiz/due-cards?deckId={deck.Id}");

        var cards = await response.Content.ReadFromJsonAsync<List<LeitnerQuizDueCardResponse>>();
        Assert.That(cards!.Select(c => c.Id), Is.EquivalentTo(new[] { dueCard.Id }));
    }

    [Test]
    public async Task GetDueCards_ExcludesCardsWithNullDueUtc()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();
        var dueCard = await _factory.Db.CreateLeitnerCardAsync(deck.Id, dueUtc: DateTime.UtcNow.AddMinutes(-1));
        _factory.Db.LeitnerCard.Add(new LeitnerCard { DeckId = deck.Id, BoxNumber = 0, DueUtc = null });
        await _factory.Db.SaveChangesAsync();

        var response = await _client.GetAsync($"/leitner/quiz/due-cards?deckId={deck.Id}");

        var cards = await response.Content.ReadFromJsonAsync<List<LeitnerQuizDueCardResponse>>();
        Assert.That(cards!.Select(c => c.Id), Is.EquivalentTo(new[] { dueCard.Id }));
    }

    [Test]
    public async Task GetDueCards_OrdersResultsByDueUtcAscending()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();
        var now = DateTime.UtcNow;
        var third = await _factory.Db.CreateLeitnerCardAsync(deck.Id, dueUtc: now.AddMinutes(-1));
        var first = await _factory.Db.CreateLeitnerCardAsync(deck.Id, dueUtc: now.AddMinutes(-30));
        var second = await _factory.Db.CreateLeitnerCardAsync(deck.Id, dueUtc: now.AddMinutes(-10));

        var response = await _client.GetAsync($"/leitner/quiz/due-cards?deckId={deck.Id}");

        var cards = await response.Content.ReadFromJsonAsync<List<LeitnerQuizDueCardResponse>>();
        Assert.That(cards!.Select(c => c.Id), Is.EqualTo(new[] { first.Id, second.Id, third.Id }));
    }

    [Test]
    public async Task GetDueCards_ReturnsCorrectBoxNumberAndProperties()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();
        var properties = new JsonObject { ["_canonicalName"] = "France", [".population"] = 68000000 };
        var card = await _factory.Db.CreateLeitnerCardAsync(deck.Id, properties: properties, boxNumber: 2, dueUtc: DateTime.UtcNow.AddMinutes(-1));

        var response = await _client.GetAsync($"/leitner/quiz/due-cards?deckId={deck.Id}");

        var cards = await response.Content.ReadFromJsonAsync<List<LeitnerQuizDueCardResponse>>();
        var found = cards!.Single(c => c.Id == card.Id);
        Assert.That(found.BoxNumber, Is.EqualTo(2));
        Assert.That(found.Properties["_canonicalName"]!.ToString(), Is.EqualTo("France"));
    }

    [Test]
    public async Task GetDueCards_PropertiesComeBackInFaceOrder()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();
        var properties = new JsonObject
        {
            ["#flag"] = new JsonObject { ["id"] = Guid.NewGuid().ToString(), ["alt_text"] = "The flag of France" },
            ["_canonicalName"] = "France",
            [".population"] = 68000000
        };
        var card = await _factory.Db.CreateLeitnerCardAsync(deck.Id, properties: properties, dueUtc: DateTime.UtcNow.AddMinutes(-1));

        var response = await _client.GetAsync($"/leitner/quiz/due-cards?deckId={deck.Id}");

        var cards = await response.Content.ReadFromJsonAsync<List<LeitnerQuizDueCardResponse>>();
        var found = cards!.Single(c => c.Id == card.Id);
        Assert.That(found.Properties.Select(p => p.Key), Is.EqualTo(new[] { "#flag", "_canonicalName", ".population" }));
    }

    [Test]
    public async Task GetDueCards_ResponseShape_OnlyContainsIdBoxNumberAndProperties()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();
        await _factory.Db.CreateLeitnerCardAsync(deck.Id, dueUtc: DateTime.UtcNow.AddMinutes(-1));

        var response = await _client.GetAsync($"/leitner/quiz/due-cards?deckId={deck.Id}");

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var keys = doc.RootElement[0].EnumerateObject().Select(p => p.Name);
        Assert.That(keys, Is.EquivalentTo(new[] { "id", "boxNumber", "properties" }));
    }
}

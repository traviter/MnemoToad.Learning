using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MnemoToad.Learning.Api.Contracts;
using MnemoToad.Learning.Data.Entities;
using MnemoToad.Learning.Tests.TestSupport;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Json;

namespace MnemoToad.Learning.Tests.SystemTests;

[TestFixture]
public class LeitnerDecksControllerSystemTests
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
    public async Task GetById_WhenNotFound_Returns404()
    {
        var response = await _client.GetAsync($"/leitner/decks/{Guid.NewGuid()}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Create_ThenGetById_RoundTripsThroughTheRealStack()
    {
        var createResponse = await _client.PostAsJsonAsync("/leitner/decks", new LeitnerDeckRequest("World Capitals", "Country name on the front, capital city on the back."));
        var created = await createResponse.Content.ReadFromJsonAsync<LeitnerDeck>();

        var getResponse = await _client.GetAsync($"/leitner/decks/{created!.Id}");

        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var fetched = await getResponse.Content.ReadFromJsonAsync<LeitnerDeck>();
        Assert.That(fetched!.Name, Is.EqualTo("World Capitals"));
    }

    [Test]
    public async Task Create_WithBlankName_Returns400WithValidationErrors()
    {
        var response = await _client.PostAsJsonAsync("/leitner/decks", new LeitnerDeckRequest("", null));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.That(problem!.Errors, Contains.Key("Name"));
    }

    [Test]
    public async Task Create_WithDuplicateName_SucceedsForBoth()
    {
        var name = $"Duplicate Deck {Guid.NewGuid()}";

        var firstResponse = await _client.PostAsJsonAsync("/leitner/decks", new LeitnerDeckRequest(name, null));
        var secondResponse = await _client.PostAsJsonAsync("/leitner/decks", new LeitnerDeckRequest(name, null));

        Assert.That(firstResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(secondResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var first = await firstResponse.Content.ReadFromJsonAsync<LeitnerDeck>();
        var second = await secondResponse.Content.ReadFromJsonAsync<LeitnerDeck>();
        Assert.That(first!.Id, Is.Not.EqualTo(second!.Id));
    }

    [Test]
    public async Task Update_WhenNotFound_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/leitner/decks/{Guid.NewGuid()}", new LeitnerDeckRequest("World Capitals", null));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Delete_WhenNotFound_Returns404()
    {
        var response = await _client.DeleteAsync($"/leitner/decks/{Guid.NewGuid()}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Delete_WhenExists_Returns204AndRemovesIt()
    {
        var leitnerDeck = await _factory.Db.CreateLeitnerDeckAsync();

        var deleteResponse = await _client.DeleteAsync($"/leitner/decks/{leitnerDeck.Id}");

        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(await _factory.Db.LeitnerDeck.AsNoTracking().FirstOrDefaultAsync(d => d.Id == leitnerDeck.Id), Is.Null);
    }

    // Red until LeitnerCard persistence exists at all, and likely to stay red even after that:
    // per MnemoToad.Knowledge's precedent (see its CLAUDE.md, knowledge_node_media's ON DELETE
    // CASCADE), a deck-to-card cascade is expected to live purely in the Postgres DbUp DDL, which
    // this SQLite-backed MockableAppDbContext never enforces — only a real-Postgres Karate scenario
    // (leitnerdeck.feature, "Delete a deck that still has cards cascades to delete them") can
    // actually prove this works. Kept here anyway as the target behavior.
    [Test]
    public async Task Delete_WhenDeckHasCards_AlsoDeletesTheCards()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();
        var createCardResponse = await _client.PostAsJsonAsync("/leitner/cards", new LeitnerCardsBulkCreateRequest(
            deck.Id, new List<LeitnerCardCreateRequest> { new(NodeId: null, Properties: new() { ["_canonicalName"] = "France", [".population"] = 68000000 }) }));
        var card = (await createCardResponse.Content.ReadFromJsonAsync<LeitnerCardsBulkCreateResponse>())!.Cards.Single();

        var deleteResponse = await _client.DeleteAsync($"/leitner/decks/{deck.Id}");

        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var getCardResponse = await _client.GetAsync($"/leitner/cards/{card.Id}");
        Assert.That(getCardResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}

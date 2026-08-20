using Microsoft.AspNetCore.Mvc;
using MnemoToad.Learning.Api.Contracts;
using MnemoToad.Learning.Tests.TestSupport;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace MnemoToad.Learning.Tests.SystemTests;

// Encodes the LeitnerCard API's target behavior against the real HTTP pipeline, per the approved
// Swagger contract — LeitnerCardsController currently stubs every action with 501, so these are
// expected to fail (red) until the persistence layer lands in a later phase. The one exception is
// pure request-shape validation (missing/empty DeckId, empty Cards/Properties), which
// [ApiController]'s automatic DataAnnotations check runs before the action body ever executes, so
// those cases already pass today. LeitnerDeck setup seeds straight into the in-memory DB via
// DbFixtures.CreateLeitnerDeckAsync (Deck is only a precondition here, not what's under test) —
// same "setup-only preconditions skip HTTP" rule MnemoToad.Knowledge's system tests already follow.
[TestFixture]
public class LeitnerCardsControllerSystemTests
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
        var response = await _client.GetAsync($"/leitner/cards/{Guid.NewGuid()}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Create_ThenGetById_RoundTripsPropertiesThroughTheRealStack()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();
        var properties = new Dictionary<string, object?>
        {
            ["_canonicalName"] = "France",
            [".population"] = 68000000,
            ["#flag"] = new Dictionary<string, object?> { ["id"] = Guid.NewGuid().ToString(), ["alt_text"] = "The flag of France" }
        };

        var createResponse = await _client.PostAsJsonAsync("/leitner/cards", new LeitnerCardsBulkCreateRequest(
            deck.Id, new List<LeitnerCardCreateRequest> { new(NodeId: null, Properties: properties) }));

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var created = await createResponse.Content.ReadFromJsonAsync<LeitnerCardsBulkCreateResponse>();
        var createdCard = created!.Cards.Single();

        var getResponse = await _client.GetAsync($"/leitner/cards/{createdCard.Id}");

        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var fetched = await getResponse.Content.ReadFromJsonAsync<LeitnerCardResponse>();
        Assert.That(fetched!.DeckId, Is.EqualTo(deck.Id));
        Assert.That(fetched.Properties["_canonicalName"]!.ToString(), Is.EqualTo("France"));
    }

    [Test]
    public async Task Create_NewCard_StartsAtBoxZeroImmediatelyDueAndUnreviewed()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();
        var beforeCreate = DateTime.UtcNow;

        var createResponse = await _client.PostAsJsonAsync("/leitner/cards", new LeitnerCardsBulkCreateRequest(
            deck.Id, new List<LeitnerCardCreateRequest> { new(NodeId: null, Properties: new() { ["_canonicalName"] = "France" }) }));
        var created = await createResponse.Content.ReadFromJsonAsync<LeitnerCardsBulkCreateResponse>();
        var card = created!.Cards.Single();

        Assert.That(card.BoxNumber, Is.EqualTo(0));
        Assert.That(card.DueUtc, Is.LessThanOrEqualTo(DateTime.UtcNow));
        Assert.That(card.DueUtc, Is.GreaterThanOrEqualTo(beforeCreate.AddSeconds(-1)));
        Assert.That(card.LastReviewedUtc, Is.Null);
    }

    [Test]
    public async Task Create_WithNodeId_RoundTripsNodeId()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();
        var nodeId = Guid.NewGuid();

        var createResponse = await _client.PostAsJsonAsync("/leitner/cards", new LeitnerCardsBulkCreateRequest(
            deck.Id, new List<LeitnerCardCreateRequest> { new(NodeId: nodeId, Properties: new() { ["_canonicalName"] = "France" }) }));
        var created = await createResponse.Content.ReadFromJsonAsync<LeitnerCardsBulkCreateResponse>();

        Assert.That(created!.Cards.Single().NodeId, Is.EqualTo(nodeId));
    }

    [Test]
    public async Task Create_WithoutNodeId_NodeIdIsNull()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();

        var createResponse = await _client.PostAsJsonAsync("/leitner/cards", new LeitnerCardsBulkCreateRequest(
            deck.Id, new List<LeitnerCardCreateRequest> { new(NodeId: null, Properties: new() { ["_canonicalName"] = "France" }) }));
        var created = await createResponse.Content.ReadFromJsonAsync<LeitnerCardsBulkCreateResponse>();

        Assert.That(created!.Cards.Single().NodeId, Is.Null);
    }

    [Test]
    public async Task Create_WithMultipleCards_CreatesAllOfThemInTheSameDeck()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();

        var createResponse = await _client.PostAsJsonAsync("/leitner/cards", new LeitnerCardsBulkCreateRequest(
            deck.Id, new List<LeitnerCardCreateRequest>
            {
                new(NodeId: null, Properties: new() { ["_canonicalName"] = "France" }),
                new(NodeId: null, Properties: new() { ["_canonicalName"] = "Japan" })
            }));

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var created = await createResponse.Content.ReadFromJsonAsync<LeitnerCardsBulkCreateResponse>();
        Assert.That(created!.Cards, Has.Count.EqualTo(2));
        Assert.That(created.Cards.Select(c => c.Id), Is.Unique);
        Assert.That(created.Cards, Has.All.Matches<LeitnerCardResponse>(c => c.DeckId == deck.Id));
    }

    [Test]
    public async Task Create_HasNoLocationHeader()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();

        var createResponse = await _client.PostAsJsonAsync("/leitner/cards", new LeitnerCardsBulkCreateRequest(
            deck.Id, new List<LeitnerCardCreateRequest> { new(NodeId: null, Properties: new() { ["_canonicalName"] = "France" }) }));

        Assert.That(createResponse.Headers.Location, Is.Null);
    }

    [Test]
    public async Task Create_WhenDeckDoesNotExist_Returns404()
    {
        var response = await _client.PostAsJsonAsync("/leitner/cards", new LeitnerCardsBulkCreateRequest(
            Guid.NewGuid(), new List<LeitnerCardCreateRequest> { new(NodeId: null, Properties: new() { ["_canonicalName"] = "France" }) }));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Create_WithMissingDeckId_Returns400WithValidationErrors()
    {
        var json = "{\"cards\":[{\"properties\":{\"_canonicalName\":\"France\"}}]}";

        var response = await _client.PostAsync("/leitner/cards", new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.That(problem!.Errors, Contains.Key("DeckId"));
    }

    [Test]
    public async Task Create_WithInvalidDeckIdFormat_Returns400WithValidationErrors()
    {
        var json = "{\"deckId\":\"not-a-guid\",\"cards\":[{\"properties\":{\"_canonicalName\":\"France\"}}]}";

        var response = await _client.PostAsync("/leitner/cards", new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.That(problem!.Errors, Contains.Key("$.deckId"));
    }

    [Test]
    public async Task Create_WithEmptyCardsArray_Returns400WithValidationErrors()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();

        var response = await _client.PostAsJsonAsync("/leitner/cards", new LeitnerCardsBulkCreateRequest(deck.Id, new List<LeitnerCardCreateRequest>()));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.That(problem!.Errors, Contains.Key("Cards"));
    }

    [Test]
    public async Task Create_WithCardHavingEmptyProperties_Returns400WithValidationErrors()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();

        var response = await _client.PostAsJsonAsync("/leitner/cards", new LeitnerCardsBulkCreateRequest(
            deck.Id, new List<LeitnerCardCreateRequest> { new(NodeId: null, Properties: new()) }));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.That(problem!.Errors, Contains.Key("Cards[0].Properties"));
    }

    [Test]
    public async Task GetByDeck_ReturnsOnlyCardsInThatDeck()
    {
        var deck1 = await _factory.Db.CreateLeitnerDeckAsync();
        var deck2 = await _factory.Db.CreateLeitnerDeckAsync();
        await _client.PostAsJsonAsync("/leitner/cards", new LeitnerCardsBulkCreateRequest(
            deck1.Id, new List<LeitnerCardCreateRequest> { new(NodeId: null, Properties: new() { ["_canonicalName"] = "France" }) }));
        await _client.PostAsJsonAsync("/leitner/cards", new LeitnerCardsBulkCreateRequest(
            deck2.Id, new List<LeitnerCardCreateRequest> { new(NodeId: null, Properties: new() { ["_canonicalName"] = "Japan" }) }));

        var response = await _client.GetAsync($"/leitner/cards?deckId={deck1.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var cards = await response.Content.ReadFromJsonAsync<List<LeitnerCardResponse>>();
        Assert.That(cards, Has.All.Matches<LeitnerCardResponse>(c => c.DeckId == deck1.Id));
    }

    [Test]
    public async Task GetByDeck_WithKnownDeckIdAndNoMatchingCards_ReturnsOkWithEmptyList()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();

        var response = await _client.GetAsync($"/leitner/cards?deckId={deck.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var cards = await response.Content.ReadFromJsonAsync<List<LeitnerCardResponse>>();
        Assert.That(cards, Is.Empty);
    }

    [Test]
    public async Task GetByDeck_WithUnknownDeckId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/leitner/cards?deckId={Guid.NewGuid()}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetByDeck_WithMissingDeckId_Returns400WithValidationErrors()
    {
        var response = await _client.GetAsync("/leitner/cards");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.That(problem!.Errors, Contains.Key("deckId"));
    }

    [Test]
    public async Task Delete_WhenNotFound_Returns404()
    {
        var response = await _client.DeleteAsync($"/leitner/cards/{Guid.NewGuid()}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Delete_WhenExists_AndCardHasProperties_Returns204AndRemovesIt()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();
        var createResponse = await _client.PostAsJsonAsync("/leitner/cards", new LeitnerCardsBulkCreateRequest(
            deck.Id, new List<LeitnerCardCreateRequest> { new(NodeId: null, Properties: new()
            {
                ["_canonicalName"] = "France",
                [".population"] = 68000000,
                ["#flag"] = new Dictionary<string, object?> { ["id"] = Guid.NewGuid().ToString(), ["alt_text"] = "The flag of France" }
            }) }));
        var created = await createResponse.Content.ReadFromJsonAsync<LeitnerCardsBulkCreateResponse>();
        var cardId = created!.Cards.Single().Id;

        var deleteResponse = await _client.DeleteAsync($"/leitner/cards/{cardId}");

        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var getResponse = await _client.GetAsync($"/leitner/cards/{cardId}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}

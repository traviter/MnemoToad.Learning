using Microsoft.AspNetCore.Mvc;
using MnemoToad.Learning.Api.Contracts;
using MnemoToad.Learning.Tests.TestSupport;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Json;

namespace MnemoToad.Learning.Tests.SystemTests;

// Encodes the LeitnerDeck API's target behavior against the real HTTP pipeline, per the approved
// Swagger contract — LeitnerDecksController currently stubs every action with 501, so these are
// expected to fail (red) until the persistence layer (entity/repository/DbContext) lands in a
// later phase. There's no DbContext seeding here (unlike NodeTypesControllerSystemTests, which
// seeds via MockableAppDbContext) since no LeitnerDeck entity exists yet — setup goes through the
// HTTP API itself (Create) instead.
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
        var created = await createResponse.Content.ReadFromJsonAsync<LeitnerDeckResponse>();

        var getResponse = await _client.GetAsync($"/leitner/decks/{created!.Id}");

        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var fetched = await getResponse.Content.ReadFromJsonAsync<LeitnerDeckResponse>();
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
        var first = await firstResponse.Content.ReadFromJsonAsync<LeitnerDeckResponse>();
        var second = await secondResponse.Content.ReadFromJsonAsync<LeitnerDeckResponse>();
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
        var createResponse = await _client.PostAsJsonAsync("/leitner/decks", new LeitnerDeckRequest("World Capitals", null));
        var created = await createResponse.Content.ReadFromJsonAsync<LeitnerDeckResponse>();

        var deleteResponse = await _client.DeleteAsync($"/leitner/decks/{created!.Id}");

        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var getResponse = await _client.GetAsync($"/leitner/decks/{created.Id}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}

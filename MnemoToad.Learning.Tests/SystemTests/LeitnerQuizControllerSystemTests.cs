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

    // SubmitAnswers currently stubs 501, per the approved Swagger contract -- these are expected to
    // fail (red) until the answer-processing logic lands in a later phase. The exceptions are the
    // pure request-shape validations (empty array, missing CardId/Correct, negative BoxNumber),
    // which [ApiController]'s automatic DataAnnotations check runs before the action body ever
    // executes, so those already pass today. Since the endpoint returns no body, state changes are
    // verified via a follow-up GET.

    [Test]
    public async Task SubmitAnswers_EmptyArray_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/leitner/quiz/cards/answers", new List<LeitnerAnswerRequest>());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task SubmitAnswers_MissingCardId_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/leitner/quiz/cards/answers",
            new List<LeitnerAnswerRequest> { new(null, true, null) });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task SubmitAnswers_MissingCorrect_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/leitner/quiz/cards/answers",
            new List<LeitnerAnswerRequest> { new(Guid.NewGuid(), null, null) });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task SubmitAnswers_NegativeBoxNumber_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/leitner/quiz/cards/answers",
            new List<LeitnerAnswerRequest> { new(Guid.NewGuid(), true, -1) });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task SubmitAnswers_UnknownCardId_Returns404()
    {
        var response = await _client.PostAsJsonAsync("/leitner/quiz/cards/answers",
            new List<LeitnerAnswerRequest> { new(Guid.NewGuid(), true, null) });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task SubmitAnswers_UnknownCardIdInBatch_RejectsWholeBatchNoPartialApplication()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();
        var card1 = await _factory.Db.CreateLeitnerCardAsync(deck.Id, boxNumber: 1, dueUtc: DateTime.UtcNow.AddMinutes(-1));
        var card2 = await _factory.Db.CreateLeitnerCardAsync(deck.Id, boxNumber: 2, dueUtc: DateTime.UtcNow.AddMinutes(-1));

        var response = await _client.PostAsJsonAsync("/leitner/quiz/cards/answers", new List<LeitnerAnswerRequest>
        {
            new(card1.Id, true, null),
            new(card2.Id, false, null),
            new(Guid.NewGuid(), true, null)
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var fetched1 = await (await _client.GetAsync($"/leitner/cards/{card1.Id}")).Content.ReadFromJsonAsync<LeitnerCard>();
        var fetched2 = await (await _client.GetAsync($"/leitner/cards/{card2.Id}")).Content.ReadFromJsonAsync<LeitnerCard>();
        Assert.That(fetched1!.BoxNumber, Is.EqualTo(1));
        Assert.That(fetched2!.BoxNumber, Is.EqualTo(2));
    }

    [Test]
    public async Task SubmitAnswers_Correct_IncrementsBoxRecomputesDueDateAndTimestamp()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();
        var card = await _factory.Db.CreateLeitnerCardAsync(deck.Id, boxNumber: 1, dueUtc: DateTime.UtcNow.AddMinutes(-1));
        await _factory.Db.CreateLeitnerScheduleAsync(boxNumber: 2, intervalHours: 72, varianceHours: 6);
        var beforeAnswer = DateTime.UtcNow;

        var response = await _client.PostAsJsonAsync("/leitner/quiz/cards/answers",
            new List<LeitnerAnswerRequest> { new(card.Id, true, null) });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var fetched = await (await _client.GetAsync($"/leitner/cards/{card.Id}")).Content.ReadFromJsonAsync<LeitnerCard>();
        Assert.That(fetched!.BoxNumber, Is.EqualTo(2));
        Assert.That(fetched.DueUtc, Is.GreaterThanOrEqualTo(beforeAnswer.AddHours(66)));
        Assert.That(fetched.DueUtc, Is.LessThanOrEqualTo(DateTime.UtcNow.AddHours(78)));
        Assert.That(fetched.LastReviewedUtc, Is.GreaterThanOrEqualTo(beforeAnswer.AddSeconds(-1)));
        Assert.That(fetched.LastReviewedUtc, Is.LessThanOrEqualTo(DateTime.UtcNow));
    }

    [Test]
    public async Task SubmitAnswers_Incorrect_ResetsBoxToZeroAndReschedulesImmediately()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();
        var card = await _factory.Db.CreateLeitnerCardAsync(deck.Id, boxNumber: 3, dueUtc: DateTime.UtcNow.AddMinutes(-1));
        await _factory.Db.CreateLeitnerScheduleAsync(boxNumber: 0, intervalHours: 0, varianceHours: 0);
        var beforeAnswer = DateTime.UtcNow;

        var response = await _client.PostAsJsonAsync("/leitner/quiz/cards/answers",
            new List<LeitnerAnswerRequest> { new(card.Id, false, null) });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var fetched = await (await _client.GetAsync($"/leitner/cards/{card.Id}")).Content.ReadFromJsonAsync<LeitnerCard>();
        Assert.That(fetched!.BoxNumber, Is.EqualTo(0));
        Assert.That(fetched.DueUtc, Is.LessThanOrEqualTo(DateTime.UtcNow));
        Assert.That(fetched.LastReviewedUtc, Is.GreaterThanOrEqualTo(beforeAnswer.AddSeconds(-1)));
    }

    [Test]
    public async Task SubmitAnswers_ExplicitBoxNumberOverride_WinsRegardlessOfCorrect()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();
        var incorrectCard = await _factory.Db.CreateLeitnerCardAsync(deck.Id, boxNumber: 1, dueUtc: DateTime.UtcNow.AddMinutes(-1));
        var correctCard = await _factory.Db.CreateLeitnerCardAsync(deck.Id, boxNumber: 1, dueUtc: DateTime.UtcNow.AddMinutes(-1));
        await _factory.Db.CreateLeitnerScheduleAsync(boxNumber: 5, intervalHours: 720, varianceHours: 48);

        var response = await _client.PostAsJsonAsync("/leitner/quiz/cards/answers", new List<LeitnerAnswerRequest>
        {
            new(incorrectCard.Id, false, 5),
            new(correctCard.Id, true, 5)
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var fetchedIncorrect = await (await _client.GetAsync($"/leitner/cards/{incorrectCard.Id}")).Content.ReadFromJsonAsync<LeitnerCard>();
        var fetchedCorrect = await (await _client.GetAsync($"/leitner/cards/{correctCard.Id}")).Content.ReadFromJsonAsync<LeitnerCard>();
        Assert.That(fetchedIncorrect!.BoxNumber, Is.EqualTo(5));
        Assert.That(fetchedCorrect!.BoxNumber, Is.EqualTo(5));
    }

    [Test]
    public async Task SubmitAnswers_Batch_AppliesEachAnswerIndependently()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();
        var correctCard = await _factory.Db.CreateLeitnerCardAsync(deck.Id, boxNumber: 1, dueUtc: DateTime.UtcNow.AddMinutes(-1));
        var incorrectCard = await _factory.Db.CreateLeitnerCardAsync(deck.Id, boxNumber: 2, dueUtc: DateTime.UtcNow.AddMinutes(-1));
        var overriddenCard = await _factory.Db.CreateLeitnerCardAsync(deck.Id, boxNumber: 1, dueUtc: DateTime.UtcNow.AddMinutes(-1));
        await _factory.Db.CreateLeitnerScheduleAsync(boxNumber: 0, intervalHours: 0, varianceHours: 0);
        await _factory.Db.CreateLeitnerScheduleAsync(boxNumber: 2, intervalHours: 72, varianceHours: 6);
        await _factory.Db.CreateLeitnerScheduleAsync(boxNumber: 7, intervalHours: 2880, varianceHours: 192);

        var response = await _client.PostAsJsonAsync("/leitner/quiz/cards/answers", new List<LeitnerAnswerRequest>
        {
            new(correctCard.Id, true, null),
            new(incorrectCard.Id, false, null),
            new(overriddenCard.Id, true, 7)
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var fetchedCorrect = await (await _client.GetAsync($"/leitner/cards/{correctCard.Id}")).Content.ReadFromJsonAsync<LeitnerCard>();
        var fetchedIncorrect = await (await _client.GetAsync($"/leitner/cards/{incorrectCard.Id}")).Content.ReadFromJsonAsync<LeitnerCard>();
        var fetchedOverridden = await (await _client.GetAsync($"/leitner/cards/{overriddenCard.Id}")).Content.ReadFromJsonAsync<LeitnerCard>();
        Assert.That(fetchedCorrect!.BoxNumber, Is.EqualTo(2));
        Assert.That(fetchedIncorrect!.BoxNumber, Is.EqualTo(0));
        Assert.That(fetchedOverridden!.BoxNumber, Is.EqualTo(7));
    }

    [Test]
    public async Task SubmitAnswers_ExplicitBoxNumberAboveHighestConfiguredBox_ClampsToHighestBox()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();
        var card = await _factory.Db.CreateLeitnerCardAsync(deck.Id, boxNumber: 1, dueUtc: DateTime.UtcNow.AddMinutes(-1));
        await _factory.Db.CreateLeitnerScheduleAsync(boxNumber: 0, intervalHours: 0, varianceHours: 0);
        await _factory.Db.CreateLeitnerScheduleAsync(boxNumber: 1, intervalHours: 24, varianceHours: 2);
        await _factory.Db.CreateLeitnerScheduleAsync(boxNumber: 2, intervalHours: 72, varianceHours: 6);
        await _factory.Db.CreateLeitnerScheduleAsync(boxNumber: 3, intervalHours: 168, varianceHours: 12);

        var response = await _client.PostAsJsonAsync("/leitner/quiz/cards/answers",
            new List<LeitnerAnswerRequest> { new(card.Id, true, 99) });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var fetched = await (await _client.GetAsync($"/leitner/cards/{card.Id}")).Content.ReadFromJsonAsync<LeitnerCard>();
        Assert.That(fetched!.BoxNumber, Is.EqualTo(3));
    }

    [Test]
    public async Task SubmitAnswers_ThenGetDueCards_CorrectAnswerRemovesCardFromDueListWhenRescheduledToFuture()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();
        var card = await _factory.Db.CreateLeitnerCardAsync(deck.Id, boxNumber: 1, dueUtc: DateTime.UtcNow.AddMinutes(-1));
        await _factory.Db.CreateLeitnerScheduleAsync(boxNumber: 2, intervalHours: 72, varianceHours: 6);

        var answerResponse = await _client.PostAsJsonAsync("/leitner/quiz/cards/answers",
            new List<LeitnerAnswerRequest> { new(card.Id, true, null) });
        Assert.That(answerResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var dueCardsResponse = await _client.GetAsync($"/leitner/quiz/due-cards?deckId={deck.Id}");
        var dueCards = await dueCardsResponse.Content.ReadFromJsonAsync<List<LeitnerQuizDueCardResponse>>();
        Assert.That(dueCards!.Select(c => c.Id), Does.Not.Contain(card.Id));
    }

    [Test]
    public async Task SubmitAnswers_CorrectAtHighestConfiguredBox_StaysAtHighestBox()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();
        var card = await _factory.Db.CreateLeitnerCardAsync(deck.Id, boxNumber: 3, dueUtc: DateTime.UtcNow.AddMinutes(-1));
        await _factory.Db.CreateLeitnerScheduleAsync(boxNumber: 0, intervalHours: 0, varianceHours: 0);
        await _factory.Db.CreateLeitnerScheduleAsync(boxNumber: 1, intervalHours: 24, varianceHours: 2);
        await _factory.Db.CreateLeitnerScheduleAsync(boxNumber: 2, intervalHours: 72, varianceHours: 6);
        await _factory.Db.CreateLeitnerScheduleAsync(boxNumber: 3, intervalHours: 168, varianceHours: 12);
        var beforeAnswer = DateTime.UtcNow;

        var response = await _client.PostAsJsonAsync("/leitner/quiz/cards/answers",
            new List<LeitnerAnswerRequest> { new(card.Id, true, null) });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var fetched = await (await _client.GetAsync($"/leitner/cards/{card.Id}")).Content.ReadFromJsonAsync<LeitnerCard>();
        Assert.That(fetched!.BoxNumber, Is.EqualTo(3));
        Assert.That(fetched.DueUtc, Is.GreaterThanOrEqualTo(beforeAnswer.AddHours(156)));
        Assert.That(fetched.DueUtc, Is.LessThanOrEqualTo(DateTime.UtcNow.AddHours(180)));
    }

    [Test]
    public async Task SubmitAnswers_ThenGetDueCards_IncorrectAnswerKeepsCardDueImmediately()
    {
        var deck = await _factory.Db.CreateLeitnerDeckAsync();
        var card = await _factory.Db.CreateLeitnerCardAsync(deck.Id, boxNumber: 3, dueUtc: DateTime.UtcNow.AddMinutes(-1));
        await _factory.Db.CreateLeitnerScheduleAsync(boxNumber: 0, intervalHours: 0, varianceHours: 0);

        var answerResponse = await _client.PostAsJsonAsync("/leitner/quiz/cards/answers",
            new List<LeitnerAnswerRequest> { new(card.Id, false, null) });
        Assert.That(answerResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var dueCardsResponse = await _client.GetAsync($"/leitner/quiz/due-cards?deckId={deck.Id}");
        var dueCards = await dueCardsResponse.Content.ReadFromJsonAsync<List<LeitnerQuizDueCardResponse>>();
        Assert.That(dueCards!.Select(c => c.Id), Does.Contain(card.Id));
    }
}

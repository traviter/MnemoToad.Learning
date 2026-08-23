using MnemoToad.Learning.Data.DbUtil;
using MnemoToad.Learning.Data.Entities;
using MnemoToad.Learning.Data.Entities.Operations;
using MnemoToad.Learning.Data.Repositories;
using MnemoToad.Learning.Tests.TestSupport;
using NUnit.Framework;
using System.Text.Json.Nodes;

namespace MnemoToad.Learning.Tests.Repositories;

[TestFixture]
public class LeitnerQuizRepositoryTests
{
    private MockableAppDbContext _db = null!;
    private LeitnerQuizRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new MockableAppDbContext();
        var faceMapper = new LeitnerCardFaceJsonMapper();
        _repository = new LeitnerQuizRepository(
            _db,
            new CompositeJsonMapper<LeitnerCardFace>(faceMapper),
            new LeitnerCardFaceRepository(_db));
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private async Task<LeitnerDeck> CreateDeckAsync() => await _db.CreateLeitnerDeckAsync();

    [Test]
    public async Task GetDueByDeckAsync_WithNoCards_ReturnsEmptyList()
    {
        var deck = await CreateDeckAsync();

        var fetched = await _repository.GetDueByDeckAsync(deck.Id);

        Assert.That(fetched, Is.Empty);
    }

    [Test]
    public async Task GetDueByDeckAsync_ExcludesCardsNotYetDue()
    {
        var deck = await CreateDeckAsync();
        var dueCard = await _db.CreateLeitnerCardAsync(deck.Id, dueUtc: DateTime.UtcNow.AddMinutes(-1));
        await _db.CreateLeitnerCardAsync(deck.Id, dueUtc: DateTime.UtcNow.AddMinutes(10));

        var fetched = await _repository.GetDueByDeckAsync(deck.Id);

        Assert.That(fetched.Select(c => c.Id), Is.EqualTo(new[] { dueCard.Id }));
    }

    [Test]
    public async Task GetDueByDeckAsync_ExcludesCardsWithNullDueUtc()
    {
        var deck = await CreateDeckAsync();
        var dueCard = await _db.CreateLeitnerCardAsync(deck.Id, dueUtc: DateTime.UtcNow.AddMinutes(-1));
        _db.LeitnerCard.Add(new LeitnerCard { DeckId = deck.Id, BoxNumber = 0, DueUtc = null });
        await _db.SaveChangesAsync();

        var fetched = await _repository.GetDueByDeckAsync(deck.Id);

        Assert.That(fetched.Select(c => c.Id), Is.EqualTo(new[] { dueCard.Id }));
    }

    [Test]
    public async Task GetDueByDeckAsync_OrdersResultsByDueUtcAscending()
    {
        var deck = await CreateDeckAsync();
        var now = DateTime.UtcNow;
        var third = await _db.CreateLeitnerCardAsync(deck.Id, dueUtc: now.AddMinutes(-1));
        var first = await _db.CreateLeitnerCardAsync(deck.Id, dueUtc: now.AddMinutes(-30));
        var second = await _db.CreateLeitnerCardAsync(deck.Id, dueUtc: now.AddMinutes(-10));

        var fetched = await _repository.GetDueByDeckAsync(deck.Id);

        Assert.That(fetched.Select(c => c.Id), Is.EqualTo(new[] { first.Id, second.Id, third.Id }));
    }

    [Test]
    public async Task GetDueByDeckAsync_ReturnsOnlyCardsInThatDeck()
    {
        var deck1 = await CreateDeckAsync();
        var deck2 = await CreateDeckAsync();
        var card1 = await _db.CreateLeitnerCardAsync(deck1.Id, dueUtc: DateTime.UtcNow.AddMinutes(-1));
        await _db.CreateLeitnerCardAsync(deck2.Id, dueUtc: DateTime.UtcNow.AddMinutes(-1));

        var fetched = await _repository.GetDueByDeckAsync(deck1.Id);

        Assert.That(fetched.Select(c => c.Id), Is.EqualTo(new[] { card1.Id }));
    }

    [Test]
    public async Task GetDueByDeckAsync_WhenCardHasNoFaceRows_ReturnsEmptyProperties()
    {
        var deck = await CreateDeckAsync();
        var card = new LeitnerCard { DeckId = deck.Id, BoxNumber = 0, DueUtc = DateTime.UtcNow.AddMinutes(-1) };
        _db.LeitnerCard.Add(card);
        await _db.SaveChangesAsync();

        var fetched = await _repository.GetDueByDeckAsync(deck.Id);

        Assert.That(fetched.Single().Properties.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task GetDueByDeckAsync_ReturnsPropertiesInFaceOrder()
    {
        var deck = await CreateDeckAsync();
        var properties = new JsonObject { ["#flag"] = "x", ["_canonicalName"] = "France", [".population"] = 68000000 };
        var card = await _db.CreateLeitnerCardAsync(deck.Id, properties: properties, dueUtc: DateTime.UtcNow.AddMinutes(-1));

        var fetched = await _repository.GetDueByDeckAsync(deck.Id);

        Assert.That(fetched.Single(c => c.Id == card.Id).Properties.Select(p => p.Key),
            Is.EqualTo(new[] { "#flag", "_canonicalName", ".population" }));
    }

    [Test]
    public async Task SubmitAnswersAsync_UnknownCardId_ReturnsCardNotFound()
    {
        var result = await _repository.SubmitAnswersAsync(new List<LeitnerAnswerSubmission>
        {
            new(Guid.NewGuid(), true, null)
        });

        Assert.That(result, Is.EqualTo(SubmitAnswersResult.CardNotFound));
    }

    [Test]
    public async Task SubmitAnswersAsync_ExplicitBoxNumberAboveHighestConfiguredBox_ClampsToHighestBox()
    {
        var deck = await CreateDeckAsync();
        var card = await _db.CreateLeitnerCardAsync(deck.Id, boxNumber: 1);
        await _db.CreateLeitnerScheduleAsync(boxNumber: 0, intervalHours: 0, varianceHours: 0);
        await _db.CreateLeitnerScheduleAsync(boxNumber: 1, intervalHours: 24, varianceHours: 2);

        var result = await _repository.SubmitAnswersAsync(new List<LeitnerAnswerSubmission>
        {
            new(card.Id, true, 99)
        });

        Assert.That(result, Is.EqualTo(SubmitAnswersResult.Success));
        var fetched = await _db.LeitnerCard.FindAsync(card.Id);
        Assert.That(fetched!.BoxNumber, Is.EqualTo(1));
    }

    [Test]
    public async Task SubmitAnswersAsync_Correct_IncrementsBoxAndUpdatesLastReviewedUtc()
    {
        var deck = await CreateDeckAsync();
        var card = await _db.CreateLeitnerCardAsync(deck.Id, boxNumber: 1);
        await _db.CreateLeitnerScheduleAsync(boxNumber: 2, intervalHours: 72, varianceHours: 6);
        var beforeAnswer = DateTime.UtcNow;

        var result = await _repository.SubmitAnswersAsync(new List<LeitnerAnswerSubmission>
        {
            new(card.Id, true, null)
        });

        Assert.That(result, Is.EqualTo(SubmitAnswersResult.Success));
        var fetched = await _db.LeitnerCard.FindAsync(card.Id);
        Assert.That(fetched!.BoxNumber, Is.EqualTo(2));
        Assert.That(fetched.LastReviewedUtc, Is.GreaterThanOrEqualTo(beforeAnswer.AddSeconds(-1)));
        Assert.That(fetched.LastReviewedUtc, Is.LessThanOrEqualTo(DateTime.UtcNow));
    }

    [Test]
    public async Task SubmitAnswersAsync_Incorrect_ResetsBoxToZero()
    {
        var deck = await CreateDeckAsync();
        var card = await _db.CreateLeitnerCardAsync(deck.Id, boxNumber: 3);
        await _db.CreateLeitnerScheduleAsync(boxNumber: 0, intervalHours: 0, varianceHours: 0);

        var result = await _repository.SubmitAnswersAsync(new List<LeitnerAnswerSubmission>
        {
            new(card.Id, false, null)
        });

        Assert.That(result, Is.EqualTo(SubmitAnswersResult.Success));
        var fetched = await _db.LeitnerCard.FindAsync(card.Id);
        Assert.That(fetched!.BoxNumber, Is.EqualTo(0));
        Assert.That(fetched.DueUtc, Is.LessThanOrEqualTo(DateTime.UtcNow));
    }

    [Test]
    public async Task SubmitAnswersAsync_ExplicitBoxNumber_WinsRegardlessOfCorrect()
    {
        var deck = await CreateDeckAsync();
        var card = await _db.CreateLeitnerCardAsync(deck.Id, boxNumber: 1);
        await _db.CreateLeitnerScheduleAsync(boxNumber: 5, intervalHours: 720, varianceHours: 48);

        var result = await _repository.SubmitAnswersAsync(new List<LeitnerAnswerSubmission>
        {
            new(card.Id, false, 5)
        });

        Assert.That(result, Is.EqualTo(SubmitAnswersResult.Success));
        var fetched = await _db.LeitnerCard.FindAsync(card.Id);
        Assert.That(fetched!.BoxNumber, Is.EqualTo(5));
    }

    [Test]
    public async Task SubmitAnswersAsync_CorrectAtHighestConfiguredBox_StaysAtHighestBox()
    {
        var deck = await CreateDeckAsync();
        var card = await _db.CreateLeitnerCardAsync(deck.Id, boxNumber: 3);
        await _db.CreateLeitnerScheduleAsync(boxNumber: 0, intervalHours: 0, varianceHours: 0);
        await _db.CreateLeitnerScheduleAsync(boxNumber: 1, intervalHours: 24, varianceHours: 2);
        await _db.CreateLeitnerScheduleAsync(boxNumber: 2, intervalHours: 72, varianceHours: 6);
        await _db.CreateLeitnerScheduleAsync(boxNumber: 3, intervalHours: 168, varianceHours: 12);
        var beforeAnswer = DateTime.UtcNow;

        var result = await _repository.SubmitAnswersAsync(new List<LeitnerAnswerSubmission>
        {
            new(card.Id, true, null)
        });

        Assert.That(result, Is.EqualTo(SubmitAnswersResult.Success));
        var fetched = await _db.LeitnerCard.FindAsync(card.Id);
        Assert.That(fetched!.BoxNumber, Is.EqualTo(3));
        Assert.That(fetched.DueUtc, Is.GreaterThanOrEqualTo(beforeAnswer.AddHours(156)));
        Assert.That(fetched.DueUtc, Is.LessThanOrEqualTo(DateTime.UtcNow.AddHours(180)));
    }

    [Test]
    public async Task SubmitAnswersAsync_Reschedule_DueDateFallsWithinConfiguredVariance()
    {
        var deck = await CreateDeckAsync();
        var card = await _db.CreateLeitnerCardAsync(deck.Id, boxNumber: 1);
        await _db.CreateLeitnerScheduleAsync(boxNumber: 2, intervalHours: 72, varianceHours: 6);
        var beforeAnswer = DateTime.UtcNow;

        await _repository.SubmitAnswersAsync(new List<LeitnerAnswerSubmission> { new(card.Id, true, null) });

        var fetched = await _db.LeitnerCard.FindAsync(card.Id);
        Assert.That(fetched!.DueUtc, Is.GreaterThanOrEqualTo(beforeAnswer.AddHours(66)));
        Assert.That(fetched.DueUtc, Is.LessThanOrEqualTo(DateTime.UtcNow.AddHours(78)));
    }

    [Test]
    public async Task SubmitAnswersAsync_Batch_AppliesEachAnswerIndependently()
    {
        var deck = await CreateDeckAsync();
        var correctCard = await _db.CreateLeitnerCardAsync(deck.Id, boxNumber: 1);
        var incorrectCard = await _db.CreateLeitnerCardAsync(deck.Id, boxNumber: 2);
        var overriddenCard = await _db.CreateLeitnerCardAsync(deck.Id, boxNumber: 1);
        await _db.CreateLeitnerScheduleAsync(boxNumber: 0, intervalHours: 0, varianceHours: 0);
        await _db.CreateLeitnerScheduleAsync(boxNumber: 2, intervalHours: 72, varianceHours: 6);
        await _db.CreateLeitnerScheduleAsync(boxNumber: 7, intervalHours: 2880, varianceHours: 192);

        var result = await _repository.SubmitAnswersAsync(new List<LeitnerAnswerSubmission>
        {
            new(correctCard.Id, true, null),
            new(incorrectCard.Id, false, null),
            new(overriddenCard.Id, true, 7)
        });

        Assert.That(result, Is.EqualTo(SubmitAnswersResult.Success));
        Assert.That((await _db.LeitnerCard.FindAsync(correctCard.Id))!.BoxNumber, Is.EqualTo(2));
        Assert.That((await _db.LeitnerCard.FindAsync(incorrectCard.Id))!.BoxNumber, Is.EqualTo(0));
        Assert.That((await _db.LeitnerCard.FindAsync(overriddenCard.Id))!.BoxNumber, Is.EqualTo(7));
    }
}

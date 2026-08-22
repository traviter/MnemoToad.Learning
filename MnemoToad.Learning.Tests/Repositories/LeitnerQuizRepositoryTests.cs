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
}

using MnemoToad.Learning.Data.Entities;
using MnemoToad.Learning.Data.Repositories;
using MnemoToad.Learning.Tests.TestSupport;
using NUnit.Framework;
using System.Text.Json.Nodes;

namespace MnemoToad.Learning.Tests.Repositories;

[TestFixture]
public class LeitnerCardFaceRepositoryTests
{
    private MockableAppDbContext _db = null!;
    private LeitnerCardFaceRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new MockableAppDbContext();
        _repository = new LeitnerCardFaceRepository(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task GetByCardIdsAsync_ReturnsEntryForEveryRequestedId_EvenWithNoFaces()
    {
        var deck = await _db.CreateLeitnerDeckAsync();
        var card = await _db.CreateLeitnerCardAsync(deck.Id, properties: new JsonObject());

        var result = await _repository.GetByCardIdsAsync(new List<Guid> { card.Id });

        Assert.That(result.ContainsKey(card.Id), Is.True);
        Assert.That(result[card.Id], Is.Empty);
    }

    [Test]
    public async Task GetByCardIdsAsync_ReturnsFacesInFaceIndexOrder()
    {
        var deck = await _db.CreateLeitnerDeckAsync();
        var properties = new JsonObject { ["#flag"] = "x", ["_canonicalName"] = "France", [".population"] = 68000000 };
        var card = await _db.CreateLeitnerCardAsync(deck.Id, properties: properties);

        var result = await _repository.GetByCardIdsAsync(new List<Guid> { card.Id });

        Assert.That(result[card.Id].Select(f => f.PropertyPath), Is.EqualTo(new[] { "#flag", "_canonicalName", ".population" }));
    }

    [Test]
    public async Task GetByCardIdsAsync_DoesNotCrossContaminateCards()
    {
        var deck = await _db.CreateLeitnerDeckAsync();
        var card1 = await _db.CreateLeitnerCardAsync(deck.Id, properties: new JsonObject { ["_canonicalName"] = "France" });
        var card2 = await _db.CreateLeitnerCardAsync(deck.Id, properties: new JsonObject { ["_canonicalName"] = "Japan" });

        var result = await _repository.GetByCardIdsAsync(new List<Guid> { card1.Id, card2.Id });

        Assert.That(result[card1.Id].Single().Content!.GetValue<string>(), Is.EqualTo("France"));
        Assert.That(result[card2.Id].Single().Content!.GetValue<string>(), Is.EqualTo("Japan"));
    }

    [Test]
    public async Task GetByCardsAsync_ReturnsSameResultKeyedById()
    {
        var deck = await _db.CreateLeitnerDeckAsync();
        var card = await _db.CreateLeitnerCardAsync(deck.Id, properties: new JsonObject { ["_canonicalName"] = "France" });

        var result = await _repository.GetByCardsAsync(new List<LeitnerCard> { card });

        Assert.That(result[card.Id].Single().PropertyPath, Is.EqualTo("_canonicalName"));
    }
}

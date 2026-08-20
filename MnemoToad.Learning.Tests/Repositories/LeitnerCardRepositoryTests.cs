using Microsoft.EntityFrameworkCore;
using MnemoToad.Learning.Data.Entities;
using MnemoToad.Learning.Data.Repositories;
using MnemoToad.Learning.Tests.TestSupport;
using NUnit.Framework;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace MnemoToad.Learning.Tests.Repositories;

[TestFixture]
public class LeitnerCardRepositoryTests
{
    private MockableAppDbContext _db = null!;
    private LeitnerCardRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new MockableAppDbContext();
        _repository = new LeitnerCardRepository(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private async Task<LeitnerDeck> CreateDeckAsync() => await _db.CreateLeitnerDeckAsync();

    [Test]
    public async Task CreateManyAsync_PersistsCardAndItsFaces()
    {
        var deck = await CreateDeckAsync();
        var card = new LeitnerCard { DeckId = deck.Id, Properties = new JsonObject { ["_canonicalName"] = "France" } };

        var created = await _repository.CreateManyAsync(new List<LeitnerCard> { card });

        Assert.That(created, Has.Count.EqualTo(1));
        Assert.That(await _db.LeitnerCard.AsNoTracking().FirstOrDefaultAsync(c => c.Id == card.Id), Is.Not.Null);
        Assert.That(await _db.LeitnerCardFace.CountAsync(f => f.LeitnerCardId == card.Id), Is.EqualTo(1));
    }

    [Test]
    public async Task CreateManyAsync_AcceptsArbitraryContentShapes()
    {
        var deck = await CreateDeckAsync();
        var card = new LeitnerCard
        {
            DeckId = deck.Id,
            Properties = new JsonObject
            {
                ["_canonicalName"] = "France",
                [".population"] = 68000000,
                ["#flag"] = new JsonObject { ["id"] = Guid.NewGuid().ToString(), ["alt_text"] = "The flag of France" },
                [".tags"] = new JsonArray("a", "b")
            }
        };

        await _repository.CreateManyAsync(new List<LeitnerCard> { card });
        var fetched = await _repository.GetByIdAsync(card.Id);

        Assert.That(fetched!.Properties["_canonicalName"]!.GetValue<string>(), Is.EqualTo("France"));
        Assert.That(fetched.Properties[".population"]!.GetValue<int>(), Is.EqualTo(68000000));
        Assert.That(fetched.Properties["#flag"], Is.InstanceOf<JsonObject>());
        Assert.That(fetched.Properties[".tags"], Is.InstanceOf<JsonArray>());
    }

    [Test]
    public void CreateManyAsync_WithNullPropertyValue_ThrowsValidationException()
    {
        var card = new LeitnerCard { DeckId = Guid.NewGuid(), Properties = new JsonObject { ["_canonicalName"] = null } };

        Assert.ThrowsAsync<ValidationException>(() => _repository.CreateManyAsync(new List<LeitnerCard> { card }));
    }

    [Test]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        var found = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.That(found, Is.Null);
    }

    [Test]
    public async Task GetByIdAsync_WhenCardHasNoFaceRows_ReturnsEmptyProperties()
    {
        var deck = await CreateDeckAsync();
        var card = new LeitnerCard { DeckId = deck.Id, BoxNumber = 0, DueUtc = DateTime.UtcNow };
        _db.LeitnerCard.Add(card);
        await _db.SaveChangesAsync();

        var fetched = await _repository.GetByIdAsync(card.Id);

        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.Properties, Is.Not.Null);
        Assert.That(fetched.Properties.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task GetByIdAsync_WhenDueUtcIsNull_ReturnsNullDueUtc()
    {
        var deck = await CreateDeckAsync();
        var card = new LeitnerCard { DeckId = deck.Id, BoxNumber = 0, DueUtc = null };
        _db.LeitnerCard.Add(card);
        await _db.SaveChangesAsync();

        var fetched = await _repository.GetByIdAsync(card.Id);

        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.DueUtc, Is.Null);
    }

    [Test]
    public async Task GetByDeckAsync_WhenCardHasNoFaceRows_ReturnsEmptyProperties()
    {
        var deck = await CreateDeckAsync();
        var card = new LeitnerCard { DeckId = deck.Id, BoxNumber = 0, DueUtc = DateTime.UtcNow };
        _db.LeitnerCard.Add(card);
        await _db.SaveChangesAsync();

        var fetched = await _repository.GetByDeckAsync(deck.Id);

        Assert.That(fetched.Single().Properties.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task GetByIdAsync_ReturnsPropertiesInSubmittedOrder()
    {
        var deck = await CreateDeckAsync();
        var card = new LeitnerCard
        {
            DeckId = deck.Id,
            Properties = new JsonObject { ["#flag"] = "x", ["_canonicalName"] = "France", [".population"] = 68000000 }
        };
        await _repository.CreateManyAsync(new List<LeitnerCard> { card });

        var fetched = await _repository.GetByIdAsync(card.Id);

        Assert.That(fetched!.Properties.Select(p => p.Key), Is.EqualTo(new[] { "#flag", "_canonicalName", ".population" }));
    }

    [Test]
    public async Task GetByDeckAsync_ReturnsPropertiesInSubmittedOrder()
    {
        var deck = await CreateDeckAsync();
        var card = new LeitnerCard
        {
            DeckId = deck.Id,
            Properties = new JsonObject { ["#flag"] = "x", ["_canonicalName"] = "France", [".population"] = 68000000 }
        };
        await _repository.CreateManyAsync(new List<LeitnerCard> { card });

        var fetched = await _repository.GetByDeckAsync(deck.Id);

        Assert.That(fetched.Single().Properties.Select(p => p.Key), Is.EqualTo(new[] { "#flag", "_canonicalName", ".population" }));
    }

    [Test]
    public async Task GetByDeckAsync_ReturnsOnlyCardsInThatDeck()
    {
        var deck1 = await CreateDeckAsync();
        var deck2 = await CreateDeckAsync();
        var card1 = new LeitnerCard { DeckId = deck1.Id, Properties = new JsonObject { ["_canonicalName"] = "France" } };
        var card2 = new LeitnerCard { DeckId = deck2.Id, Properties = new JsonObject { ["_canonicalName"] = "Japan" } };
        await _repository.CreateManyAsync(new List<LeitnerCard> { card1, card2 });

        var fetched = await _repository.GetByDeckAsync(deck1.Id);

        Assert.That(fetched.Select(c => c.Id), Is.EqualTo(new[] { card1.Id }));
        Assert.That(fetched.Single().Properties["_canonicalName"]!.GetValue<string>(), Is.EqualTo("France"));
    }

    [Test]
    public async Task GetByDeckAsync_WithNoCards_ReturnsEmptyList()
    {
        var deck = await CreateDeckAsync();

        var fetched = await _repository.GetByDeckAsync(deck.Id);

        Assert.That(fetched, Is.Empty);
    }

    [Test]
    public async Task DeleteAsync_WhenExists_RemovesCardAndReturnsTrue()
    {
        var deck = await CreateDeckAsync();
        var card = new LeitnerCard { DeckId = deck.Id, Properties = new JsonObject { ["_canonicalName"] = "France" } };
        await _repository.CreateManyAsync(new List<LeitnerCard> { card });

        var result = await _repository.DeleteAsync(card.Id);

        Assert.That(result, Is.True);
        Assert.That(await _db.LeitnerCard.AsNoTracking().FirstOrDefaultAsync(c => c.Id == card.Id), Is.Null);
    }

    [Test]
    public async Task DeleteAsync_WhenNotFound_ReturnsFalse()
    {
        var result = await _repository.DeleteAsync(Guid.NewGuid());

        Assert.That(result, Is.False);
    }
}

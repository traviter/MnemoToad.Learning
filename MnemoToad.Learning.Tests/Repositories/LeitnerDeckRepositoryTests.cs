using Microsoft.EntityFrameworkCore;
using MnemoToad.Learning.Data.Entities;
using MnemoToad.Learning.Data.Repositories;
using MnemoToad.Learning.Tests.TestSupport;
using NUnit.Framework;

namespace MnemoToad.Learning.Tests.Repositories;

[TestFixture]
public class LeitnerDeckRepositoryTests
{
    private MockableAppDbContext _db = null!;
    private LeitnerDeckRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new MockableAppDbContext();
        _repository = new LeitnerDeckRepository(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task GetAllAsync_ReturnsAllLeitnerDecks()
    {
        await _db.LeitnerDeck.AddRangeAsync(
            new LeitnerDeck { Id = Guid.NewGuid(), Name = "Place" },
            new LeitnerDeck { Id = Guid.NewGuid(), Name = "Animal" });
        await _db.SaveChangesAsync();

        var all = await _repository.GetAllAsync();

        Assert.That(all.Select(d => d.Name), Is.EquivalentTo(new[] { "Place", "Animal" }));
    }

    [Test]
    public async Task GetByIdAsync_WhenExists_ReturnsLeitnerDeck()
    {
        var leitnerDeck = new LeitnerDeck { Id = Guid.NewGuid(), Name = "World Capitals" };
        await _db.LeitnerDeck.AddAsync(leitnerDeck);
        await _db.SaveChangesAsync();

        var found = await _repository.GetByIdAsync(leitnerDeck.Id);

        Assert.That(found?.Name, Is.EqualTo("World Capitals"));
    }

    [Test]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        var found = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.That(found, Is.Null);
    }

    [Test]
    public async Task CreateAsync_PersistsAndReturnsLeitnerDeck()
    {
        var leitnerDeck = new LeitnerDeck { Id = Guid.NewGuid(), Name = "World Capitals" };

        var created = await _repository.CreateAsync(leitnerDeck);

        Assert.That(created, Is.SameAs(leitnerDeck));
        Assert.That(await _db.LeitnerDeck.FindAsync(leitnerDeck.Id), Is.Not.Null);
    }

    [Test]
    public async Task CreateAsync_WithDuplicateName_PersistsBothSuccessfully()
    {
        var name = "World Capitals";

        var first = await _repository.CreateAsync(new LeitnerDeck { Id = Guid.NewGuid(), Name = name });
        var second = await _repository.CreateAsync(new LeitnerDeck { Id = Guid.NewGuid(), Name = name });

        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
        Assert.That(await _db.LeitnerDeck.CountAsync(d => d.Name == name), Is.EqualTo(2));
    }

    [Test]
    public async Task UpdateAsync_WhenNotFound_ReturnsNull()
    {
        var updated = await _repository.UpdateAsync(new LeitnerDeck { Id = Guid.NewGuid(), Name = "World Capitals" });

        Assert.That(updated, Is.Null);
    }

    [Test]
    public async Task UpdateAsync_WithValidData_UpdatesAndReturnsLeitnerDeck()
    {
        var leitnerDeck = new LeitnerDeck { Id = Guid.NewGuid(), Name = "World Capitals", Description = "Old" };
        await _db.LeitnerDeck.AddAsync(leitnerDeck);
        await _db.SaveChangesAsync();

        var updated = await _repository.UpdateAsync(new LeitnerDeck { Id = leitnerDeck.Id, Name = "World Capitals", Description = "New description" });

        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Description, Is.EqualTo("New description"));
    }

    [Test]
    public async Task DeleteAsync_WhenExists_RemovesLeitnerDeckAndReturnsTrue()
    {
        var leitnerDeck = new LeitnerDeck { Id = Guid.NewGuid(), Name = "World Capitals" };
        await _db.LeitnerDeck.AddAsync(leitnerDeck);
        await _db.SaveChangesAsync();

        var result = await _repository.DeleteAsync(leitnerDeck.Id);

        Assert.That(result, Is.True);
        Assert.That(await _db.LeitnerDeck.AsNoTracking().FirstOrDefaultAsync(d => d.Id == leitnerDeck.Id), Is.Null);
    }

    [Test]
    public async Task DeleteAsync_WhenNotFound_ReturnsFalse()
    {
        var result = await _repository.DeleteAsync(Guid.NewGuid());

        Assert.That(result, Is.False);
    }
}

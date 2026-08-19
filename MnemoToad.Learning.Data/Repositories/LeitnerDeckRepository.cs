using Microsoft.EntityFrameworkCore;
using MnemoToad.Learning.Data.Entities;

namespace MnemoToad.Learning.Data.Repositories;

public class LeitnerDeckRepository : ILeitnerDeckRepository
{
    private readonly IAppDbContext _db;

    public LeitnerDeckRepository(IAppDbContext db)
    {
        _db = db;
    }

    public Task<List<LeitnerDeck>> GetAllAsync() =>
        _db.LeitnerDeck.ToListAsync();

    public async Task<LeitnerDeck?> GetByIdAsync(Guid id) =>
        await _db.LeitnerDeck.FindAsync(id);

    public async Task<LeitnerDeck> CreateAsync(LeitnerDeck leitnerDeck)
    {
        _db.LeitnerDeck.Add(leitnerDeck);
        await _db.SaveChangesAsync();
        return leitnerDeck;
    }

    public async Task<LeitnerDeck?> UpdateAsync(LeitnerDeck leitnerDeck)
    {
        var existing = await GetByIdAsync(leitnerDeck.Id);
        if (existing is null) return null;

        existing.Name = leitnerDeck.Name;
        existing.Description = leitnerDeck.Description;
        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id) =>
        await _db.ExecuteDeleteAsync(_db.LeitnerDeck.Where(d => d.Id == id)) > 0;
}

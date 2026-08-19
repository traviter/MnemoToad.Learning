using Microsoft.EntityFrameworkCore;
using MnemoToad.Learning.Data.Entities;

namespace MnemoToad.Learning.Data;

public interface IAppDbContext : IDisposable
{
    DbSet<LeitnerDeck> LeitnerDeck { get; }

    Task<int> SaveChangesAsync();
    Task<int> ExecuteDeleteAsync<TEntity>(IQueryable<TEntity> query) where TEntity : class;
}

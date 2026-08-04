using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MnemoToad.Learning.Data;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public Task<int> SaveChangesAsync() => SaveChangesAsync(CancellationToken.None);

    public async Task<int> ExecuteDeleteAsync<TEntity>(IQueryable<TEntity> query) where TEntity : class
    {
        try
        {
            return await query.ExecuteDeleteAsync();
        }
        catch (PostgresException ex)
        {
            throw new DbUpdateException(ex.Message, ex);
        }
    }
}

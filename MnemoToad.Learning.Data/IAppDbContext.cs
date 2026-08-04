namespace MnemoToad.Learning.Data;

public interface IAppDbContext : IDisposable
{
    Task<int> SaveChangesAsync();
    Task<int> ExecuteDeleteAsync<TEntity>(IQueryable<TEntity> query) where TEntity : class;
}

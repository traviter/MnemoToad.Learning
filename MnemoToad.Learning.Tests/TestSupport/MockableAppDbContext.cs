using Microsoft.Data.Sqlite;
using Moq;
using MnemoToad.Learning.Data;

namespace MnemoToad.Learning.Tests.TestSupport;

internal sealed class MockableAppDbContext : IAppDbContext
{
    private readonly IAppDbContext _wrapped;
    private readonly SqliteConnection? _connection;
    private readonly Mock<IAppDbContext> _mock;

    public MockableAppDbContext(IAppDbContext? wrapped = null)
    {
        if (wrapped is null)
        {
            (var context, _connection) = SqliteAppDbContext.Create();
            _wrapped = context;
        }
        else
        {
            _wrapped = wrapped;
        }

        _mock = new Mock<IAppDbContext>();
        _mock.Setup(db => db.SaveChangesAsync()).Returns(() => _wrapped.SaveChangesAsync());
    }

    public Task<int> SaveChangesAsync() => _mock.Object.SaveChangesAsync();
    public Task<int> ExecuteDeleteAsync<TEntity>(IQueryable<TEntity> query) where TEntity : class =>
        _wrapped.ExecuteDeleteAsync(query);

    public MockableAppDbContext ThrowOnSaveChanges(Exception exception)
    {
        _mock.Setup(db => db.SaveChangesAsync()).ThrowsAsync(exception);
        return this;
    }

    public void Dispose()
    {
        _wrapped.Dispose();
        _connection?.Dispose();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MnemoToad.Learning.Data.Entities;
using Npgsql;
using System.Text.Json.Nodes;

namespace MnemoToad.Learning.Data;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<LeitnerDeck> LeitnerDeck => Set<LeitnerDeck>();
    public DbSet<LeitnerCard> LeitnerCard => Set<LeitnerCard>();
    public DbSet<LeitnerCardFace> LeitnerCardFace => Set<LeitnerCardFace>();
    public DbSet<LeitnerSchedule> LeitnerSchedule => Set<LeitnerSchedule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LeitnerCard>().Ignore(c => c.Properties);

        var jsonNodeComparer = new ValueComparer<JsonNode?>(
            (a, b) => (a == null && b == null) || (a != null && b != null && a.ToJsonString() == b.ToJsonString()),
            v => v == null ? 0 : v.ToJsonString().GetHashCode(),
            v => v == null ? null : JsonNode.Parse(v.ToJsonString()));

        modelBuilder.Entity<LeitnerCardFace>().HasKey(f => new { f.LeitnerCardId, f.PropertyPath });

        modelBuilder.Entity<LeitnerCardFace>()
            .Property(f => f.Content)
            .HasColumnType("jsonb")
            .HasConversion(v => v.ToJsonString(), v => JsonNode.Parse(v)!)
            .Metadata.SetValueComparer(jsonNodeComparer);
    }

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

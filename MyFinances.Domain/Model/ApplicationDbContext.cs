namespace MyFinances.Domain.Model;

using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Asset> Assets { get; set; }
    public DbSet<MarketData> MarketData { get; set; }
    public DbSet<Movement> Movements { get; set; }
    public DbSet<Possession> Possessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MarketData>()
        .Property(m => m.Close)
        .HasPrecision(10, 2);

        base.OnModelCreating(modelBuilder);
    }
}

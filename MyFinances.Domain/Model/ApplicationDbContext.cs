namespace MyFinances.Domain.Model;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Asset> Assets { get; set; }
    public DbSet<Currency> Currencies { get; set; }
    public DbSet<CurrencyExchangeRate> CurrencyExchangeRates { get; set; }
    public DbSet<MarketData> MarketData { get; set; }
    public DbSet<Movement> Movements { get; set; }
    public DbSet<Possession> Possessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // MarketData configuration
        modelBuilder.Entity<MarketData>()
            .Property(m => m.Close)
            .HasPrecision(10, 2);

        modelBuilder.Entity<MarketData>()
            .HasIndex(md => new { md.AssetId, md.Date })
            .IsUnique();

        // Currency configuration
        modelBuilder.Entity<Currency>()
            .HasIndex(c => c.Code)
            .IsUnique();

        modelBuilder.Entity<Currency>()
            .HasIndex(c => c.IsDefault);

        // Asset-Currency relationship (optional)
        modelBuilder.Entity<Asset>()
            .HasOne(a => a.Currency)
            .WithMany(c => c.Assets)
            .HasForeignKey(a => a.CurrencyId)
            .OnDelete(DeleteBehavior.SetNull);

        // MarketData-Currency relationship (optional)
        modelBuilder.Entity<MarketData>()
            .HasOne(md => md.Currency)
            .WithMany(c => c.MarketData)
            .HasForeignKey(md => md.CurrencyId)
            .OnDelete(DeleteBehavior.SetNull);

        // Movement-Currency relationship (optional)
        modelBuilder.Entity<Movement>()
            .HasOne(m => m.Currency)
            .WithMany(c => c.Movements)
            .HasForeignKey(m => m.CurrencyId)
            .OnDelete(DeleteBehavior.SetNull);

        // Possession-Currency relationship (optional)
        modelBuilder.Entity<Possession>()
            .HasOne(p => p.Currency)
            .WithMany(c => c.Possessions)
            .HasForeignKey(p => p.CurrencyId)
            .OnDelete(DeleteBehavior.SetNull);

        // CurrencyExchangeRate configuration
        modelBuilder.Entity<CurrencyExchangeRate>()
            .HasOne(cer => cer.FromCurrency)
            .WithMany(c => c.FromExchangeRates)
            .HasForeignKey(cer => cer.FromCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CurrencyExchangeRate>()
            .HasOne(cer => cer.ToCurrency)
            .WithMany(c => c.ToExchangeRates)
            .HasForeignKey(cer => cer.ToCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index for performance on exchange rates
        modelBuilder.Entity<CurrencyExchangeRate>()
            .HasIndex(cer => new { cer.FromCurrencyId, cer.ToCurrencyId, cer.IsLatest });

        modelBuilder.Entity<CurrencyExchangeRate>()
            .HasIndex(cer => new { cer.Date, cer.IsLatest });

        // Ensure unique latest rate per currency pair
        modelBuilder.Entity<CurrencyExchangeRate>()
            .HasIndex(cer => new { cer.FromCurrencyId, cer.ToCurrencyId, cer.IsLatest })
            .HasFilter("IsLatest = 1")
            .IsUnique();

        // Seed common currencies
        SeedCurrencies(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private static void SeedCurrencies(ModelBuilder modelBuilder)
    {
        var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        var currencies = new []
        {
            new Currency { Id = 1, Code = "USD", Name = "US Dollar", Symbol = "$", IsDefault = true, IsActive = true, CreatedAt = baseDate },
            new Currency { Id = 2, Code = "EUR", Name = "Euro", Symbol = "€", IsDefault = false, IsActive = true, CreatedAt = baseDate },
            new Currency { Id = 3, Code = "GBP", Name = "British Pound Sterling", Symbol = "£", IsDefault = false, IsActive = true, CreatedAt = baseDate },
            new Currency { Id = 4, Code = "JPY", Name = "Japanese Yen", Symbol = "¥", IsDefault = false, IsActive = true, CreatedAt = baseDate },
            new Currency { Id = 5, Code = "CAD", Name = "Canadian Dollar", Symbol = "C$", IsDefault = false, IsActive = true, CreatedAt = baseDate },
            new Currency { Id = 6, Code = "AUD", Name = "Australian Dollar", Symbol = "A$", IsDefault = false, IsActive = true, CreatedAt = baseDate },
            new Currency { Id = 7, Code = "CHF", Name = "Swiss Franc", Symbol = "CHF", IsDefault = false, IsActive = true, CreatedAt = baseDate },
            new Currency { Id = 8, Code = "CNY", Name = "Chinese Yuan", Symbol = "¥", IsDefault = false, IsActive = true, CreatedAt = baseDate },
            new Currency { Id = 9, Code = "ARS", Name = "Argentine Peso", Symbol = "$", IsDefault = false, IsActive = true, CreatedAt = baseDate },
            new Currency { Id = 10, Code = "BRL", Name = "Brazilian Real", Symbol = "R$", IsDefault = false, IsActive = true, CreatedAt = baseDate },
            new Currency { Id = 11, Code = "MXN", Name = "Mexican Peso", Symbol = "$", IsDefault = false, IsActive = true, CreatedAt = baseDate },
            new Currency { Id = 12, Code = "CLP", Name = "Chilean Peso", Symbol = "$", IsDefault = false, IsActive = true, CreatedAt = baseDate },
            new Currency { Id = 13, Code = "COP", Name = "Colombian Peso", Symbol = "$", IsDefault = false, IsActive = true, CreatedAt = baseDate },
            new Currency { Id = 14, Code = "PEN", Name = "Peruvian Sol", Symbol = "S/", IsDefault = false, IsActive = true, CreatedAt = baseDate },
            new Currency { Id = 15, Code = "UYU", Name = "Uruguayan Peso", Symbol = "$U", IsDefault = false, IsActive = true, CreatedAt = baseDate },
            new Currency { Id = 16, Code = "INR", Name = "Indian Rupee", Symbol = "₹", IsDefault = false, IsActive = true, CreatedAt = baseDate },
            new Currency { Id = 17, Code = "KRW", Name = "South Korean Won", Symbol = "₩", IsDefault = false, IsActive = true, CreatedAt = baseDate },
            new Currency { Id = 18, Code = "SGD", Name = "Singapore Dollar", Symbol = "S$", IsDefault = false, IsActive = true, CreatedAt = baseDate },
            new Currency { Id = 19, Code = "HKD", Name = "Hong Kong Dollar", Symbol = "HK$", IsDefault = false, IsActive = true, CreatedAt = baseDate },
            new Currency { Id = 20, Code = "NZD", Name = "New Zealand Dollar", Symbol = "NZ$", IsDefault = false, IsActive = true, CreatedAt = baseDate }
        };

        modelBuilder.Entity<Currency>().HasData(currencies);
    }
}

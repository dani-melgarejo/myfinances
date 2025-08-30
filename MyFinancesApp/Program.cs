using Microsoft.EntityFrameworkCore;
using MyFinances.Domain.Model;
using MyFinances.Logic.Configuration;
using MyFinances.Logic.Interfaces;
using MyFinances.Logic.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient<HistoricService>();

// Configurar opciones de configuración
builder.Services.Configure<AppConfig>(builder.Configuration);

// Registrar tus servicios
builder.Services.AddScoped<IMovementService, MovementService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IHistoricService, HistoricService>();
builder.Services.AddScoped<IMarketDataService, MarketDataService>();
builder.Services.AddScoped<IPossessionService, PossessionService>();
builder.Services.AddScoped<IPortfolioReportService, PortfolioReportService>();

// Configurar Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)
    ));



var app = builder.Build();
// Add services to the container.

// Aplicar migraciones automáticamente en desarrollo
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated(); // O usar context.Database.Migrate() si prefieres migraciones
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
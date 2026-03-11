using Microsoft.EntityFrameworkCore;
using EmailTracker.Data;
using EmailTracker.Repositories.Interfaces;
using EmailTracker.Repositories.Implementations;
using EmailTracker.Services;

var builder = WebApplication.CreateBuilder(args);

// ── MVC ──────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson();

// ── SQLite via EF Core ───────────────────────────────────────────
// Connection string is a static path configured in appsettings.json.
// To change the database location, update "DefaultConnection" in appsettings.json.
var dbPath = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(dbPath));

// ── Repositories (scoped = per-request) ─────────────────────────
builder.Services.AddScoped<IRunRepository,     RunRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<ISenderRepository,  SenderRepository>();
builder.Services.AddScoped<IRatingRepository,  RatingRepository>();

// ── Services ─────────────────────────────────────────────────────
builder.Services.AddScoped<IRunService,     RunService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<ISenderService,  SenderService>();
builder.Services.AddScoped<IRatingService,  RatingService>();

var app = builder.Build();

// ── Pipeline ──────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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

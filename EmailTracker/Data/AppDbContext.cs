using Microsoft.EntityFrameworkCore;
using EmailTracker.Models;

namespace EmailTracker.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Core tables
    public DbSet<Rating>  Ratings  { get; set; }
    public DbSet<Run>     Runs     { get; set; }
    public DbSet<Sender>  Senders  { get; set; }
    public DbSet<Message> Messages { get; set; }

    // SQLite views (keyless / read-only)
    public DbSet<VSenderWithRating>  VSenderWithRatings  { get; set; }
    public DbSet<VMessageWithSender> VMessageWithSenders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Rating ──────────────────────────────────────────────────
        modelBuilder.Entity<Rating>(e =>
        {
            e.ToTable("rating");
            e.HasIndex(r => r.RatingName).IsUnique();
            e.HasIndex(r => r.SortOrder).IsUnique();
        });

        // ── Run ─────────────────────────────────────────────────────
        modelBuilder.Entity<Run>(e =>
        {
            e.ToTable("run");
        });

        // ── Sender ──────────────────────────────────────────────────
        modelBuilder.Entity<Sender>(e =>
        {
            e.ToTable("sender");
            e.HasIndex(s => s.EmailAddress).IsUnique();
            e.HasIndex(s => s.RatingId);

            e.HasOne(s => s.Rating)
             .WithMany(r => r.Senders)
             .HasForeignKey(s => s.RatingId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Message ─────────────────────────────────────────────────
        modelBuilder.Entity<Message>(e =>
        {
            e.ToTable("message");
            e.HasIndex(m => m.SenderId);
            e.HasIndex(m => m.RunId);
            e.HasIndex(m => m.InternalDate);

            e.HasOne(m => m.Run)
             .WithMany(r => r.Messages)
             .HasForeignKey(m => m.RunId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(m => m.Sender)
             .WithMany(s => s.Messages)
             .HasForeignKey(m => m.SenderId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Views (keyless) ─────────────────────────────────────────
        modelBuilder.Entity<VSenderWithRating>(e =>
        {
            e.HasNoKey();
            e.ToView("v_sender_with_rating");
        });

        modelBuilder.Entity<VMessageWithSender>(e =>
        {
            e.HasNoKey();
            e.ToView("v_message_with_sender");
        });
    }
}

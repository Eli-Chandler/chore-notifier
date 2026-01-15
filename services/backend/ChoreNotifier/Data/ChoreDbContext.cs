using ChoreNotifier.Models;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Data;

public class ChoreDbContext : DbContext
{
    public ChoreDbContext(DbContextOptions<ChoreDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Chore> Chores { get; set; }
    public DbSet<ChoreAssignee> ChoreAssignees { get; set; }
    public DbSet<ChoreOccurrence> ChoreOccurrences { get; set; }
    public DbSet<NotificationAttempt> NotificationAttempts { get; set; }
    public DbSet<NotificationMethod> NotificationMethods { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Notification as owned by NotificationAttempt
        modelBuilder.Entity<NotificationAttempt>()
            .OwnsOne(na => na.Notification);

        // Configure NotificationMethod TPH inheritance
        modelBuilder.Entity<NotificationMethod>()
            .HasDiscriminator(m => m.Type)
            .HasValue<ConsoleMethod>(NotificationType.Console)
            .HasValue<NtfyMethod>(NotificationType.Ntfy);

        // Configure User -> NotificationMethod relationship (1-1)
        modelBuilder.Entity<User>()
            .HasOne(u => u.NotificationPreference)
            .WithOne()
            .HasForeignKey<NotificationMethod>("UserId")
            .IsRequired(false);
    }
}

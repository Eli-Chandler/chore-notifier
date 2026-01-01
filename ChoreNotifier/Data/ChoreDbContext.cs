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
}

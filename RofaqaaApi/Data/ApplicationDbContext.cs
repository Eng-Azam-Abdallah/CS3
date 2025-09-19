using Microsoft.EntityFrameworkCore;
using RofaqaaApi.Models;

namespace RofaqaaApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ExpenseShare> ExpenseShares => Set<ExpenseShare>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // تكوين العلاقات والقيود

        // User
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // Group Member
        modelBuilder.Entity<GroupMember>()
            .HasIndex(gm => new { gm.GroupId, gm.UserId })
            .IsUnique();

        // Expense Share
        modelBuilder.Entity<ExpenseShare>()
            .HasIndex(es => new { es.ExpenseId, es.GroupMemberId })
            .IsUnique();

        // Payment
        modelBuilder.Entity<Payment>()
            .HasIndex(p => new { p.ExpenseId, p.GroupMemberId });
    }
}
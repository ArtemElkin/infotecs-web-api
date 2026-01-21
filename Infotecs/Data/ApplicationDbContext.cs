using Infotecs.Models;
using Microsoft.EntityFrameworkCore;

namespace Infotecs.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Value> Values { get; set; }
    public DbSet<Result> Results { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Result>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FileName);
            entity.HasIndex(e => e.MinDate);
        });

        modelBuilder.Entity<Value>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FileName);
            entity.HasIndex(e => e.Date);
            entity.HasOne(e => e.Result)
                .WithMany(r => r.Values)
                .HasForeignKey(e => e.ResultId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}


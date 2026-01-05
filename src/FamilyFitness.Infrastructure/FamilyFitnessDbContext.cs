using FamilyFitness.Domain;
using Microsoft.EntityFrameworkCore;

namespace FamilyFitness.Infrastructure;

public class FamilyFitnessDbContext : DbContext
{
    public FamilyFitnessDbContext(DbContextOptions<FamilyFitnessDbContext> options) 
        : base(options)
    {
    }

    public DbSet<WorkoutTypeEntity> WorkoutTypes => Set<WorkoutTypeEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkoutTypeEntity>(entity =>
        {
            entity.ToTable("workout_types");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .IsRequired();
            
            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.HasIndex(e => e.Name)
                .IsUnique();
        });
    }
}

public class WorkoutTypeEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

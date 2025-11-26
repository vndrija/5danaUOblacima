using System;
using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
    {
    }

    public DbSet<Student> Students { get; set; } = null!;
    public DbSet<Canteen> Canteens { get; set; } = null!;

    public DbSet<Reservation> Reservations { get; set; } = null!;
    public DbSet<WorkingHour> WorkingHours { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Student>()
            .HasIndex(s => s.Email)
            .IsUnique();

        modelBuilder.Entity<Student>()
            .Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        modelBuilder.Entity<Student>()
            .Property(s => s.Email)
            .IsRequired()
            .HasMaxLength(200);

        modelBuilder.Entity<Canteen>()
            .HasIndex(c => c.Name)
            .IsUnique();

        modelBuilder.Entity<Canteen>()
            .Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        modelBuilder.Entity<Canteen>()
            .Property(c => c.Location)
            .IsRequired()
            .HasMaxLength(500);

        modelBuilder.Entity<Canteen>()
            .Property(c => c.Capacity)
            .IsRequired();

        modelBuilder.Entity<WorkingHour>()
            .Property(wh => wh.From)
            .IsRequired()
            .HasMaxLength(5);

        modelBuilder.Entity<WorkingHour>()
            .Property(wh => wh.To)
            .IsRequired()
            .HasMaxLength(5);

        modelBuilder.Entity<WorkingHour>()
            .Property(wh => wh.Meal)
            .HasConversion<string>();

        modelBuilder.Entity<Reservation>()
            .Property(r => r.Date)
            .IsRequired();

        modelBuilder.Entity<Reservation>()
            .Property(r => r.Time)
            .IsRequired()
            .HasMaxLength(5);

        modelBuilder.Entity<Reservation>()
            .Property(r => r.Duration)
            .IsRequired();

        modelBuilder.Entity<Reservation>()
            .Property(r => r.Status)
            .HasConversion<string>()
            .IsRequired();

        modelBuilder.Entity<Canteen>()
            .HasMany(c => c.WorkingHours)
            .WithOne(wh => wh.Canteen)
            .HasForeignKey(wh => wh.CanteenId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Canteen>()
            .HasMany(c => c.Reservations)
            .WithOne(r => r.Canteen)
            .HasForeignKey(r => r.CanteenId)
            .OnDelete(DeleteBehavior.Cascade); // Cascade delete reservations when canteen is deleted

        modelBuilder.Entity<Student>()
            .HasMany(s => s.Reservations)
            .WithOne(r => r.Student)
            .HasForeignKey(r => r.StudentId)
            .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete

        modelBuilder.Entity<Reservation>()
            .HasIndex(r => new { r.CanteenId, r.Date, r.Status });

        modelBuilder.Entity<Reservation>()
            .HasIndex(r => new { r.StudentId, r.Date, r.Status });
    }
}

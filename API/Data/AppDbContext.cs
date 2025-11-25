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

        modelBuilder.Entity<Canteen>()
            .HasMany(c => c.WorkingHours)
            .WithOne(wh => wh.Canteen)
            .HasForeignKey(wh => wh.CanteenId)
            .OnDelete(DeleteBehavior.Cascade);

          modelBuilder.Entity<Student>()
            .HasIndex(s => s.Email)
            .IsUnique();

        modelBuilder.Entity<Canteen>()
            .HasIndex(c => c.Name)
            .IsUnique();

        modelBuilder.Entity<Canteen>()
            .HasMany(c => c.WorkingHours)
            .WithOne(wh => wh.Canteen)
            .HasForeignKey(wh => wh.CanteenId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Canteen>()
            .HasMany(c => c.Reservations)
            .WithOne(r => r.Canteen)
            .HasForeignKey(r => r.CanteenId)
            .OnDelete(DeleteBehavior.Cascade);


        modelBuilder.Entity<Reservation>()
            .Property(r => r.Status)
            .HasConversion<string>();
    }
}

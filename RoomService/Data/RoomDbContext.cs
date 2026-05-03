using Microsoft.EntityFrameworkCore;
using RoomService.Models;

namespace RoomService.Data;

public class RoomDbContext : DbContext
{
    public RoomDbContext(DbContextOptions<RoomDbContext> options) : base(options) { }

    public DbSet<Room> Rooms => Set<Room>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.Floor).HasMaxLength(50);
            entity.Property(e => e.PricePerHour).HasColumnType("decimal(10,2)");
        });
    }

    public void SeedData()
    {
        if (Rooms.Any()) return; // already seeded

        Rooms.AddRange(
            new Room { Name = "Conference Room A", Location = "Building 1", Floor = "Floor 1", Capacity = 10, HasProjector = true, HasWhiteboard = true, HasVideoConference = false, PricePerHour = 50, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Room { Name = "Board Room", Location = "Building 1", Floor = "Floor 3", Capacity = 20, HasProjector = true, HasWhiteboard = true, HasVideoConference = true, PricePerHour = 150, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Room { Name = "Meeting Pod 1", Location = "Building 2", Floor = "Floor 2", Capacity = 4, HasProjector = false, HasWhiteboard = true, HasVideoConference = false, PricePerHour = 25, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Room { Name = "Innovation Lab", Location = "Building 2", Floor = "Floor 1", Capacity = 15, HasProjector = true, HasWhiteboard = true, HasVideoConference = true, PricePerHour = 100, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Room { Name = "Training Room", Location = "Building 3", Floor = "Floor 1", Capacity = 30, HasProjector = true, HasWhiteboard = false, HasVideoConference = false, PricePerHour = 75, IsActive = true, CreatedAt = DateTime.UtcNow }
        );
        SaveChanges();
    }
}

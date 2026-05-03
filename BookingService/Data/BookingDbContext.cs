using BookingService.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Data;

public class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options) { }

    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BookedByUserId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.BookedByName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.BookedByEmail).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Confirmed");
            entity.Property(e => e.CancellationReason).HasMaxLength(500);

            // Index for fast conflict queries
            entity.HasIndex(e => new { e.RoomId, e.StartTime, e.EndTime })
                  .HasDatabaseName("IX_Bookings_RoomId_TimeRange");

            entity.HasIndex(e => e.BookedByUserId)
                  .HasDatabaseName("IX_Bookings_UserId");
        });
    }
}

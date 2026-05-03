namespace BookingService.Models;

public class Booking
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string BookedByUserId { get; set; } = string.Empty;
    public string BookedByName { get; set; } = string.Empty;
    public string BookedByEmail { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;         // e.g. "Sprint Planning"
    public string? Notes { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = "Confirmed";         // Confirmed | Cancelled
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
}

// DTOs
public record CreateBookingRequest(
    int RoomId,
    string BookedByUserId,
    string BookedByName,
    string BookedByEmail,
    string Title,
    string? Notes,
    DateTime StartTime,
    DateTime EndTime
);

public record CancelBookingRequest(string Reason);

public record BookingResponse(
    int Id,
    int RoomId,
    string BookedByName,
    string BookedByEmail,
    string Title,
    string? Notes,
    DateTime StartTime,
    DateTime EndTime,
    string Status,
    double DurationHours,
    DateTime CreatedAt
);

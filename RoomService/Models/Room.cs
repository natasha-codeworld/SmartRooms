namespace RoomService.Models;

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Floor { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public bool HasProjector { get; set; }
    public bool HasWhiteboard { get; set; }
    public bool HasVideoConference { get; set; }
    public decimal PricePerHour { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

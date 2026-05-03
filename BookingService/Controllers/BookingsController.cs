using BookingService.Data;
using BookingService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly BookingDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(
        BookingDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<BookingsController> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    // GET /api/bookings
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var bookings = await _context.Bookings
            .OrderByDescending(b => b.StartTime)
            .Select(b => MapToResponse(b))
            .ToListAsync();
        return Ok(bookings);
    }

    // GET /api/bookings/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking is null)
            return NotFound(new { message = $"Booking {id} not found." });
        return Ok(MapToResponse(booking));
    }

    // GET /api/bookings/user/{userId}
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(string userId)
    {
        var bookings = await _context.Bookings
            .Where(b => b.BookedByUserId == userId)
            .OrderByDescending(b => b.StartTime)
            .Select(b => MapToResponse(b))
            .ToListAsync();
        return Ok(bookings);
    }

    // GET /api/bookings/room/{roomId}
    [HttpGet("room/{roomId}")]
    public async Task<IActionResult> GetByRoom(int roomId)
    {
        var bookings = await _context.Bookings
            .Where(b => b.RoomId == roomId && b.Status == "Confirmed")
            .OrderBy(b => b.StartTime)
            .Select(b => MapToResponse(b))
            .ToListAsync();
        return Ok(bookings);
    }

    // GET /api/bookings/room/{roomId}/schedule?date=2025-06-01
    [HttpGet("room/{roomId}/schedule")]
    public async Task<IActionResult> GetRoomSchedule(int roomId, [FromQuery] DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = date.Date.AddDays(1);

        var schedule = await _context.Bookings
            .Where(b =>
                b.RoomId == roomId &&
                b.Status == "Confirmed" &&
                b.StartTime < endOfDay &&
                b.EndTime > startOfDay)
            .OrderBy(b => b.StartTime)
            .Select(b => new
            {
                b.Id,
                b.Title,
                b.BookedByName,
                b.StartTime,
                b.EndTime,
                DurationHours = (b.EndTime - b.StartTime).TotalHours
            })
            .ToListAsync();

        return Ok(new
        {
            RoomId = roomId,
            Date = date.Date,
            TotalBookings = schedule.Count,
            Schedule = schedule
        });
    }

    // GET /api/bookings/conflicts?roomId=1&start=...&end=...
    [HttpGet("conflicts")]
    public async Task<IActionResult> CheckConflicts(
        [FromQuery] int roomId,
        [FromQuery] DateTime start,
        [FromQuery] DateTime end,
        [FromQuery] int? excludeBookingId = null)
    {
        var query = _context.Bookings.Where(b =>
            b.RoomId == roomId &&
            b.Status == "Confirmed" &&
            b.StartTime < end &&
            b.EndTime > start);

        if (excludeBookingId.HasValue)
            query = query.Where(b => b.Id != excludeBookingId.Value);

        var conflicts = await query
            .Select(b => new { b.Id, b.Title, b.StartTime, b.EndTime, b.BookedByName })
            .ToListAsync();

        return Ok(new
        {
            HasConflict = conflicts.Any(),
            Conflicts = conflicts
        });
    }

    // POST /api/bookings
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Validate times
        if (request.StartTime >= request.EndTime)
            return BadRequest(new { message = "Start time must be before end time." });

        if (request.StartTime < DateTime.UtcNow)
            return BadRequest(new { message = "Cannot book a room in the past." });

        // Validate room exists via RoomService
        var roomServiceUrl = _config["RoomServiceUrl"] ?? "http://roomservice:8080";
        var client = _httpClientFactory.CreateClient();
        var roomResponse = await client.GetAsync($"{roomServiceUrl}/api/rooms/{request.RoomId}");
        if (!roomResponse.IsSuccessStatusCode)
            return BadRequest(new { message = $"Room {request.RoomId} does not exist or is inactive." });

        // === CONFLICT DETECTION (the key business logic) ===
        // A conflict exists when:
        //   existing.StartTime < newBooking.EndTime   AND
        //   existing.EndTime   > newBooking.StartTime
        // This catches ALL overlap cases: full overlap, partial start, partial end
        var hasConflict = await _context.Bookings.AnyAsync(b =>
            b.RoomId == request.RoomId &&
            b.Status == "Confirmed" &&
            b.StartTime < request.EndTime &&
            b.EndTime > request.StartTime);

        if (hasConflict)
        {
            // Return details about what's conflicting
            var conflictingBookings = await _context.Bookings
                .Where(b =>
                    b.RoomId == request.RoomId &&
                    b.Status == "Confirmed" &&
                    b.StartTime < request.EndTime &&
                    b.EndTime > request.StartTime)
                .Select(b => new { b.Id, b.Title, b.StartTime, b.EndTime, b.BookedByName })
                .ToListAsync();

            return Conflict(new
            {
                message = "Room is already booked for that time slot.",
                conflictingBookings
            });
        }

        var booking = new Booking
        {
            RoomId = request.RoomId,
            BookedByUserId = request.BookedByUserId,
            BookedByName = request.BookedByName,
            BookedByEmail = request.BookedByEmail,
            Title = request.Title,
            Notes = request.Notes,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = "Confirmed",
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Booking created: Room {RoomId}, by {User}, from {Start} to {End}",
            booking.RoomId, booking.BookedByName, booking.StartTime, booking.EndTime);

        return CreatedAtAction(nameof(GetById), new { id = booking.Id }, MapToResponse(booking));
    }

    // PUT /api/bookings/{id}/cancel
    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelBookingRequest request)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking is null)
            return NotFound(new { message = $"Booking {id} not found." });

        if (booking.Status == "Cancelled")
            return BadRequest(new { message = "Booking is already cancelled." });

        if (booking.StartTime <= DateTime.UtcNow)
            return BadRequest(new { message = "Cannot cancel a booking that has already started." });

        booking.Status = "Cancelled";
        booking.CancellationReason = request.Reason;
        booking.CancelledAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Booking {Id} cancelled. Reason: {Reason}", id, request.Reason);
        return Ok(new { message = "Booking cancelled successfully.", bookingId = id });
    }

    // GET /api/bookings/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var now = DateTime.UtcNow;
        var stats = new
        {
            TotalBookings = await _context.Bookings.CountAsync(),
            ConfirmedBookings = await _context.Bookings.CountAsync(b => b.Status == "Confirmed"),
            CancelledBookings = await _context.Bookings.CountAsync(b => b.Status == "Cancelled"),
            UpcomingBookings = await _context.Bookings.CountAsync(b => b.Status == "Confirmed" && b.StartTime > now),
            MostBookedRoomId = await _context.Bookings
                .Where(b => b.Status == "Confirmed")
                .GroupBy(b => b.RoomId)
                .OrderByDescending(g => g.Count())
                .Select(g => (int?)g.Key)
                .FirstOrDefaultAsync()
        };
        return Ok(stats);
    }

    private static BookingResponse MapToResponse(Booking b) => new(
        b.Id, b.RoomId, b.BookedByName, b.BookedByEmail,
        b.Title, b.Notes, b.StartTime, b.EndTime, b.Status,
        (b.EndTime - b.StartTime).TotalHours, b.CreatedAt
    );
}

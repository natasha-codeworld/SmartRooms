using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomService.Data;
using RoomService.Models;

namespace RoomService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly RoomDbContext _context;
    private readonly ILogger<RoomsController> _logger;

    public RoomsController(RoomDbContext context, ILogger<RoomsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET /api/rooms
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var rooms = await _context.Rooms
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync();
        return Ok(rooms);
    }

    // GET /api/rooms/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var room = await _context.Rooms.FindAsync(id);
        if (room is null || !room.IsActive)
            return NotFound(new { message = $"Room {id} not found." });
        return Ok(room);
    }

    // GET /api/rooms/available?start=2025-06-01T09:00:00&end=2025-06-01T11:00:00&capacity=5
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable(
        [FromQuery] DateTime? start,
        [FromQuery] DateTime? end,
        [FromQuery] int? capacity)
    {
        var query = _context.Rooms.Where(r => r.IsActive).AsQueryable();

        if (capacity.HasValue)
            query = query.Where(r => r.Capacity >= capacity.Value);

        var rooms = await query.OrderBy(r => r.Capacity).ToListAsync();
        return Ok(rooms);
    }

    // POST /api/rooms
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Room room)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        room.CreatedAt = DateTime.UtcNow;
        room.IsActive = true;
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Room created: {Name} (ID: {Id})", room.Name, room.Id);
        return CreatedAtAction(nameof(GetById), new { id = room.Id }, room);
    }

    // PUT /api/rooms/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Room updated)
    {
        var room = await _context.Rooms.FindAsync(id);
        if (room is null) return NotFound(new { message = $"Room {id} not found." });

        room.Name = updated.Name;
        room.Location = updated.Location;
        room.Floor = updated.Floor;
        room.Capacity = updated.Capacity;
        room.HasProjector = updated.HasProjector;
        room.HasWhiteboard = updated.HasWhiteboard;
        room.HasVideoConference = updated.HasVideoConference;
        room.PricePerHour = updated.PricePerHour;

        await _context.SaveChangesAsync();
        return Ok(room);
    }

    // DELETE /api/rooms/{id}  (soft delete)
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var room = await _context.Rooms.FindAsync(id);
        if (room is null) return NotFound(new { message = $"Room {id} not found." });

        room.IsActive = false;  // soft delete
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

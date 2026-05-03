using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserService.Data;
using UserService.Models;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;

    public AuthController(UserDbContext context, IConfiguration config, ILogger<AuthController> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (await _context.Users.AnyAsync(u => u.Email == request.Email.ToLower()))
            return Conflict(new { message = "An account with this email already exists." });

        var user = new User
        {
            Name = request.Name,
            Email = request.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Department = request.Department,
            Role = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("New user registered: {Email}", user.Email);

        return Ok(new
        {
            message = "Registration successful.",
            userId = user.Id,
            name = user.Name,
            email = user.Email
        });
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower() && u.IsActive);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        _logger.LogInformation("User logged in: {Email}", user.Email);

        return Ok(new
        {
            token,
            userId = user.Id.ToString(),
            name = user.Name,
            email = user.Email,
            role = user.Role,
            expiresIn = "8 hours"
        });
    }

    // GET /api/auth/users  (admin view - list all users)
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users
            .Where(u => u.IsActive)
            .Select(u => new UserResponse(u.Id, u.Name, u.Email, u.Department, u.Role, u.CreatedAt))
            .ToListAsync();
        return Ok(users);
    }

    // GET /api/auth/users/{id}
    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null || !user.IsActive)
            return NotFound(new { message = "User not found." });

        return Ok(new UserResponse(user.Id, user.Name, user.Email, user.Department, user.Role, user.CreatedAt));
    }

    private string GenerateJwtToken(User user)
    {
        var jwtKey = _config["JwtKey"] ?? throw new InvalidOperationException("JwtKey not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("department", user.Department),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["JwtIssuer"] ?? "SmartRooms",
            audience: _config["JwtAudience"] ?? "SmartRoomsUsers",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

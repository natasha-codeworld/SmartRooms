namespace UserService.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User";       // User | Admin
    public string Department { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}

public record RegisterRequest(
    string Name,
    string Email,
    string Password,
    string Department
);

public record LoginRequest(string Email, string Password);

public record UserResponse(
    int Id,
    string Name,
    string Email,
    string Department,
    string Role,
    DateTime CreatedAt
);

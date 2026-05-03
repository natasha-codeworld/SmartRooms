using Microsoft.EntityFrameworkCore;
using UserService.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SmartRooms - User Service", Version = "v1" });
});
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var retries = 15;
    while (retries > 0)
    {
        try
        {
            logger.LogInformation("Connecting to User DB...");
            db.Database.EnsureCreated();
            logger.LogInformation("User DB ready.");
            break;
        }
        catch (Exception ex)
        {
            retries--;
            logger.LogWarning("User DB not ready. Retrying in 5s... ({R} left). {E}", retries, ex.Message);
            Thread.Sleep(5000);
        }
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();

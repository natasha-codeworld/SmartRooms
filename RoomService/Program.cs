using Microsoft.EntityFrameworkCore;
using RoomService.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SmartRooms - Room Service", Version = "v1" });
});
builder.Services.AddDbContext<RoomDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RoomDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var retries = 15;
    while (retries > 0)
    {
        try
        {
            logger.LogInformation("Connecting to Room DB...");
            db.Database.EnsureCreated();
            db.SeedData();
            logger.LogInformation("Room DB ready.");
            break;
        }
        catch (Exception ex)
        {
            retries--;
            logger.LogWarning("Room DB not ready. Retrying in 5s... ({R} left). {E}", retries, ex.Message);
            Thread.Sleep(5000);
        }
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();

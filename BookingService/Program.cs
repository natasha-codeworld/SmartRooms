using BookingService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SmartRooms - Booking Service", Version = "v1" });
});
builder.Services.AddHttpClient();
builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var retries = 15;
    while (retries > 0)
    {
        try
        {
            logger.LogInformation("Connecting to Booking DB...");
            db.Database.EnsureCreated();
            logger.LogInformation("Booking DB ready.");
            break;
        }
        catch (Exception ex)
        {
            retries--;
            logger.LogWarning("Booking DB not ready. Retrying in 5s... ({R} left). {E}", retries, ex.Message);
            Thread.Sleep(5000);
        }
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();

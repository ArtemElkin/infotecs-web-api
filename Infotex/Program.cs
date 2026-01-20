using Microsoft.EntityFrameworkCore;
using Infotex.Data;
using Infotex.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Infotex API",
        Version = "v1",
        Description = "WebAPI для работы с timescale данными результатов обработки"
    });
});

// Настройка EF Core с PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Port=5432;Database=infotex_db;Username=postgres;Password=postgres";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Регистрация сервисов
builder.Services.AddScoped<CsvProcessingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Включаем Swagger всегда (для тестового задания это нормально)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Infotex API v1");
    c.RoutePrefix = string.Empty; // Swagger UI будет доступен на корневом пути
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Применение миграций при запуске (опционально, для разработки)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Не удалось применить миграции. Убедитесь, что база данных создана и connection string правильный.");
    }
}

app.Run();

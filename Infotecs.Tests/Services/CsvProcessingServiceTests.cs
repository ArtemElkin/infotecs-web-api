using System.Text;
using Infotecs.Data;
using Infotecs.Models;
using Infotecs.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Infotecs.Tests.Services;

public class CsvProcessingServiceTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task ProcessCsvAsync_ValidCsv_SavesToDatabase()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new CsvProcessingService(context);
        var csvContent = "Date;ExecutionTime;Value\n" +
                        "2024-01-15T10-30-45.1234Z;1.5;100.25\n" +
                        "2024-01-15T10-31-00.5678Z;2.3;150.75";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));
        var fileName = "test.csv";

        // Act
        await service.ProcessCsvAsync(stream, fileName);

        // Assert
        var results = await context.Results.ToListAsync();
        var values = await context.Values.ToListAsync();
        
        Assert.Single(results);
        Assert.Equal(2, values.Count);
        Assert.Equal(fileName, results[0].FileName);
        Assert.Equal(2, results[0].Values.Count);
    }

    [Fact]
    public async Task ProcessCsvAsync_InvalidDateFormat_ThrowsArgumentException()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new CsvProcessingService(context);
        var csvContent = "Date;ExecutionTime;Value\n" +
                        "invalid-date;1.5;100.25";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            service.ProcessCsvAsync(stream, "test.csv"));
    }

    [Fact]
    public async Task ProcessCsvAsync_DateInFuture_ThrowsArgumentException()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new CsvProcessingService(context);
        var futureDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH-mm-ss.ffffZ");
        var csvContent = $"Date;ExecutionTime;Value\n" +
                        $"{futureDate};1.5;100.25";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            service.ProcessCsvAsync(stream, "test.csv"));
        Assert.Contains("дата не может быть позже текущей", exception.Message);
    }

    [Fact]
    public async Task ProcessCsvAsync_DateBefore2000_ThrowsArgumentException()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new CsvProcessingService(context);
        var csvContent = "Date;ExecutionTime;Value\n" +
                        "1999-12-31T10-30-45.1234Z;1.5;100.25";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            service.ProcessCsvAsync(stream, "test.csv"));
        Assert.Contains("дата не может быть раньше", exception.Message);
    }

    [Fact]
    public async Task ProcessCsvAsync_NegativeExecutionTime_ThrowsArgumentException()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new CsvProcessingService(context);
        var csvContent = "Date;ExecutionTime;Value\n" +
                        "2024-01-15T10-30-45.1234Z;-1.5;100.25";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            service.ProcessCsvAsync(stream, "test.csv"));
        Assert.Contains("время выполнения не может быть меньше 0", exception.Message);
    }

    [Fact]
    public async Task ProcessCsvAsync_NegativeValue_ThrowsArgumentException()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new CsvProcessingService(context);
        var csvContent = "Date;ExecutionTime;Value\n" +
                        "2024-01-15T10-30-45.1234Z;1.5;-100.25";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            service.ProcessCsvAsync(stream, "test.csv"));
        Assert.Contains("значение показателя не может быть меньше 0", exception.Message);
    }

    [Fact]
    public async Task ProcessCsvAsync_TooManyRows_ThrowsArgumentException()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new CsvProcessingService(context);
        var csvContent = new StringBuilder("Date;ExecutionTime;Value\n");
        var date = "2024-01-15T10-30-45.1234Z";
        
        for (int i = 0; i < 10001; i++)
        {
            csvContent.AppendLine($"{date};1.5;100.25");
        }
        
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent.ToString()));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            service.ProcessCsvAsync(stream, "test.csv"));
        Assert.Contains("Количество строк должно быть от 1 до 10000", exception.Message);
    }

    [Fact]
    public async Task ProcessCsvAsync_EmptyFile_ThrowsArgumentException()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new CsvProcessingService(context);
        var csvContent = "Date;ExecutionTime;Value\n";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            service.ProcessCsvAsync(stream, "test.csv"));
        Assert.Contains("Количество строк должно быть от 1 до 10000", exception.Message);
    }

    [Fact]
    public async Task ProcessCsvAsync_ExistingFileName_OverwritesData()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new CsvProcessingService(context);
        var fileName = "test.csv";
        
        // Первая загрузка
        var csvContent1 = "Date;ExecutionTime;Value\n" +
                         "2024-01-15T10-30-45.1234Z;1.5;100.25";
        var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(csvContent1));
        await service.ProcessCsvAsync(stream1, fileName);
        
        // Вторая загрузка с тем же именем
        var csvContent2 = "Date;ExecutionTime;Value\n" +
                         "2024-01-15T10-31-00.5678Z;2.3;150.75\n" +
                         "2024-01-15T10-31-15.9012Z;1.8;120.50";
        var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(csvContent2));

        // Act
        await service.ProcessCsvAsync(stream2, fileName);

        // Assert
        var results = await context.Results.Where(r => r.FileName == fileName).ToListAsync();
        var values = await context.Values.Where(v => v.FileName == fileName).ToListAsync();
        
        Assert.Single(results); // Должна быть только одна запись Result
        Assert.Equal(2, values.Count); // Должно быть 2 значения из второго файла
    }

    [Fact]
    public async Task ProcessCsvAsync_CalculatesStatisticsCorrectly()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new CsvProcessingService(context);
        var csvContent = "Date;ExecutionTime;Value\n" +
                        "2024-01-15T10-30-00.0000Z;1.0;100.0\n" +
                        "2024-01-15T10-30-30.0000Z;2.0;200.0\n" +
                        "2024-01-15T10-31-00.0000Z;3.0;300.0";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        await service.ProcessCsvAsync(stream, "test.csv");

        // Assert
        var result = await context.Results.FirstAsync();
        
        Assert.Equal(60.0, result.DeltaTime, 1); // 60 секунд между первой и последней датой
        Assert.Equal(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc), result.MinDate);
        Assert.Equal(2.0, result.AvgExecutionTime); // (1+2+3)/3
        Assert.Equal(200.0, result.AvgValue); // (100+200+300)/3
        Assert.Equal(200.0, result.MedianValue); // Медиана из [100,200,300]
        Assert.Equal(300.0, result.MaxValue);
        Assert.Equal(100.0, result.MinValue);
    }

    [Fact]
    public async Task ProcessCsvAsync_SupportsBothDateFormatFormats()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new CsvProcessingService(context);
        
        // Формат с дефисами (из задания)
        var csvContent1 = "Date;ExecutionTime;Value\n" +
                         "2024-01-15T10-30-45.1234Z;1.5;100.25";
        var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(csvContent1));
        await service.ProcessCsvAsync(stream1, "test1.csv");
        
        // Формат с двоеточиями (стандартный ISO)
        var csvContent2 = "Date;ExecutionTime;Value\n" +
                         "2024-01-15T10:30:45.1234Z;1.5;100.25";
        var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(csvContent2));
        await service.ProcessCsvAsync(stream2, "test2.csv");

        // Assert
        var results = await context.Results.ToListAsync();
        Assert.Equal(2, results.Count); // Оба файла должны быть обработаны
    }
}


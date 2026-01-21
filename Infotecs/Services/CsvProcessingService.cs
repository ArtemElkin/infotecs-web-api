using System.Globalization;
using System.Text;
using Infotecs.Data;
using Infotecs.Models;
using Microsoft.EntityFrameworkCore;

namespace Infotecs.Services;

public class CsvProcessingService
{
    private readonly ApplicationDbContext _context;
    private const int MinRows = 1;
    private const int MaxRows = 10000;
    private static readonly DateTime MinAllowedDate = new DateTime(2000, 1, 1);

    public CsvProcessingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task ProcessCsvAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var existingResult = await _context.Results
                .FirstOrDefaultAsync(r => r.FileName == fileName, cancellationToken);
            
            if (existingResult != null)
            {
                _context.Results.Remove(existingResult);
                await _context.SaveChangesAsync(cancellationToken);
            }
            
            var values = await ParseCsvAsync(fileStream, fileName, cancellationToken);
            
            ValidateValues(values);
            
            var result = CalculateResults(values, fileName);
            
            await _context.Results.AddAsync(result, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            foreach (var value in values)
            {
                value.ResultId = result.Id;
            }

            await _context.Values.AddRangeAsync(values, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<List<Value>> ParseCsvAsync(Stream stream, string fileName, CancellationToken cancellationToken)
    {
        var values = new List<Value>();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        
        var headerLine = await reader.ReadLineAsync();
        if (headerLine == null)
        {
            throw new ArgumentException("CSV файл пустой или не содержит заголовок");
        }

        string? line;
        int lineNumber = 1; // Начинаем с 1, так как заголовок уже прочитан
        
        while ((line = await reader.ReadLineAsync()) != null)
        {
            lineNumber++;
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(';');
            if (parts.Length != 3)
            {
                throw new ArgumentException($"Строка {lineNumber}: неверное количество полей. Ожидается 3 поля, получено {parts.Length}");
            }
            
            var dateString = parts[0].Trim();
            DateTime date;
            
            if (!DateTime.TryParseExact(dateString, "yyyy-MM-ddTHH-mm-ss.ffffZ", 
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, 
                    out date) &&
                !DateTime.TryParseExact(dateString, "yyyy-MM-ddTHH:mm:ss.ffffZ", 
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, 
                    out date) &&
                !DateTime.TryParse(dateString, CultureInfo.InvariantCulture, 
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out date))
            {
                throw new ArgumentException($"Строка {lineNumber}: неверный формат даты '{dateString}'. Ожидается формат: yyyy-MM-ddTHH-mm-ss.ffffZ или yyyy-MM-ddTHH:mm:ss.ffffZ");
            }
            
            if (!double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var executionTime))
            {
                throw new ArgumentException($"Строка {lineNumber}: неверный формат времени выполнения '{parts[1]}'");
            }
            
            if (!double.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var valueAmount))
            {
                throw new ArgumentException($"Строка {lineNumber}: неверный формат значения показателя '{parts[2]}'");
            }

            values.Add(new Value
            {
                Date = date,
                ExecutionTime = executionTime,
                ValueAmount = valueAmount,
                FileName = fileName
            });
        }

        return values;
    }

    private void ValidateValues(List<Value> values)
    {
        if (values.Count < MinRows || values.Count > MaxRows)
        {
            throw new ArgumentException($"Количество строк должно быть от {MinRows} до {MaxRows}, получено {values.Count}");
        }

        var now = DateTime.UtcNow;
        for (int i = 0; i < values.Count; i++)
        {
            var value = values[i];
            var rowNumber = i + 2; // +2 потому что заголовок и нумерация с 1

            if (value.Date > now)
            {
                throw new ArgumentException($"Строка {rowNumber}: дата не может быть позже текущей");
            }

            if (value.Date < MinAllowedDate)
            {
                throw new ArgumentException($"Строка {rowNumber}: дата не может быть раньше {MinAllowedDate:yyyy-MM-dd}");
            }

            if (value.ExecutionTime < 0)
            {
                throw new ArgumentException($"Строка {rowNumber}: время выполнения не может быть меньше 0");
            }

            if (value.ValueAmount < 0)
            {
                throw new ArgumentException($"Строка {rowNumber}: значение показателя не может быть меньше 0");
            }
        }
    }

    private Result CalculateResults(List<Value> values, string fileName)
    {
        if (values.Count == 0)
        {
            throw new InvalidOperationException("Невозможно вычислить результаты для пустого списка значений");
        }

        var dates = values.Select(v => v.Date).ToList();
        var executionTimes = values.Select(v => v.ExecutionTime).ToList();
        var valueAmounts = values.Select(v => v.ValueAmount).OrderBy(v => v).ToList();

        var deltaTime = (dates.Max() - dates.Min()).TotalSeconds;
        var minDate = dates.Min();
        var avgExecutionTime = executionTimes.Average();
        var avgValue = valueAmounts.Average();
        
        double medianValue;
        if (valueAmounts.Count % 2 == 0)
        {
            medianValue = (valueAmounts[valueAmounts.Count / 2 - 1] + valueAmounts[valueAmounts.Count / 2]) / 2.0;
        }
        else
        {
            medianValue = valueAmounts[valueAmounts.Count / 2];
        }

        var maxValue = valueAmounts.Max();
        var minValue = valueAmounts.Min();

        return new Result
        {
            FileName = fileName,
            DeltaTime = deltaTime,
            MinDate = minDate,
            AvgExecutionTime = avgExecutionTime,
            AvgValue = avgValue,
            MedianValue = medianValue,
            MaxValue = maxValue,
            MinValue = minValue
        };
    }
}


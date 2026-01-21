using Infotecs.Data;
using Infotecs.DTOs;
using Infotecs.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Infotecs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    private readonly CsvProcessingService _csvProcessingService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataController> _logger;

    public DataController(
        CsvProcessingService csvProcessingService,
        ApplicationDbContext context,
        ILogger<DataController> logger)
    {
        _csvProcessingService = csvProcessingService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Загружает CSV файл, валидирует данные и сохраняет их в БД
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadCsv([FromForm] UploadCsvRequest request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest(new { error = "Файл не предоставлен или пуст" });
        }

        if (!request.File.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Файл должен иметь расширение .csv" });
        }

        try
        {
            var fileName = Path.GetFileName(request.File.FileName);
            await using var stream = request.File.OpenReadStream();
            await _csvProcessingService.ProcessCsvAsync(stream, fileName, cancellationToken);

            return Ok(new { message = $"Файл '{fileName}' успешно обработан и сохранен" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Ошибка валидации при обработке CSV");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке CSV файла");
            return StatusCode(500, new { error = "Внутренняя ошибка сервера при обработке файла" });
        }
    }

    /// <summary>
    /// Получает список записей из таблицы Results с применением фильтров
    /// </summary>
    [HttpGet("results")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ResultDto>>> GetResults([FromQuery] ResultFilterDto filter, CancellationToken cancellationToken)
    {
        var query = _context.Results.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.FileName))
        {
            query = query.Where(r => r.FileName.Contains(filter.FileName));
        }

        if (filter.MinDateFrom.HasValue)
        {
            query = query.Where(r => r.MinDate >= filter.MinDateFrom.Value);
        }

        if (filter.MinDateTo.HasValue)
        {
            query = query.Where(r => r.MinDate <= filter.MinDateTo.Value);
        }

        if (filter.AvgValueFrom.HasValue)
        {
            query = query.Where(r => r.AvgValue >= filter.AvgValueFrom.Value);
        }

        if (filter.AvgValueTo.HasValue)
        {
            query = query.Where(r => r.AvgValue <= filter.AvgValueTo.Value);
        }

        if (filter.AvgExecutionTimeFrom.HasValue)
        {
            query = query.Where(r => r.AvgExecutionTime >= filter.AvgExecutionTimeFrom.Value);
        }

        if (filter.AvgExecutionTimeTo.HasValue)
        {
            query = query.Where(r => r.AvgExecutionTime <= filter.AvgExecutionTimeTo.Value);
        }

        var results = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ResultDto
            {
                Id = r.Id,
                FileName = r.FileName,
                DeltaTime = r.DeltaTime,
                MinDate = r.MinDate,
                AvgExecutionTime = r.AvgExecutionTime,
                AvgValue = r.AvgValue,
                MedianValue = r.MedianValue,
                MaxValue = r.MaxValue,
                MinValue = r.MinValue,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(results);
    }

    /// <summary>
    /// Получает последние 10 значений, отсортированных по начальному времени запуска Date, по имени заданного файла
    /// </summary>
    [HttpGet("values/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ValueDto>>> GetLastValues(string fileName, CancellationToken cancellationToken)
    {
        var values = await _context.Values
            .Where(v => v.FileName == fileName)
            .OrderByDescending(v => v.Date)
            .Take(10)
            .Select(v => new ValueDto
            {
                Id = v.Id,
                Date = v.Date,
                ExecutionTime = v.ExecutionTime,
                ValueAmount = v.ValueAmount,
                FileName = v.FileName
            })
            .ToListAsync(cancellationToken);

        if (values.Count == 0)
        {
            return NotFound(new { error = $"Файл с именем '{fileName}' не найден" });
        }

        return Ok(values);
    }
}


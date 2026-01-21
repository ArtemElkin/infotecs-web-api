namespace Infotecs.DTOs;

public class ResultFilterDto
{
    public string? FileName { get; set; }
    public DateTime? MinDateFrom { get; set; }
    public DateTime? MinDateTo { get; set; }
    public double? AvgValueFrom { get; set; }
    public double? AvgValueTo { get; set; }
    public double? AvgExecutionTimeFrom { get; set; }
    public double? AvgExecutionTimeTo { get; set; }
}


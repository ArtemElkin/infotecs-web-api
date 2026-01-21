namespace Infotecs.DTOs;

public class ValueDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public double ExecutionTime { get; set; }
    public double ValueAmount { get; set; }
    public string FileName { get; set; } = string.Empty;
}


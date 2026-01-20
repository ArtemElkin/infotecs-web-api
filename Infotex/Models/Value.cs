namespace Infotex.Models;

public class Value
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public double ExecutionTime { get; set; }
    public double ValueAmount { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int ResultId { get; set; }
    public Result? Result { get; set; }
}


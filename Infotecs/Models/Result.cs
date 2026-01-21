namespace Infotecs.Models;

public class Result
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public double DeltaTime { get; set; }
    public DateTime MinDate { get; set; }
    public double AvgExecutionTime { get; set; }
    public double AvgValue { get; set; }
    public double MedianValue { get; set; }
    public double MaxValue { get; set; }
    public double MinValue { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<Value> Values { get; set; } = new List<Value>();
}


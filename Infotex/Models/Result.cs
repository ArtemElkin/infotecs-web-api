namespace Infotex.Models;

public class Result
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public double DeltaTime { get; set; } // дельта времени Date в секундах
    public DateTime MinDate { get; set; } // минимальная дата и время
    public double AvgExecutionTime { get; set; } // среднее время выполнения
    public double AvgValue { get; set; } // среднее значение по показателям
    public double MedianValue { get; set; } // медиана по показателям
    public double MaxValue { get; set; } // максимальное значение показателя
    public double MinValue { get; set; } // минимальное значение показателя
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<Value> Values { get; set; } = new List<Value>();
}


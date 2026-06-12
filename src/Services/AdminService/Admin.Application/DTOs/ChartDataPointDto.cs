namespace Admin.Application.DTOs;

/// <summary>
/// Generic chart-friendly data point for analytics endpoints.
/// Label is typically a date string or name.
/// Value is a numeric count.
/// </summary>
public class ChartDataPointDto
{
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
}

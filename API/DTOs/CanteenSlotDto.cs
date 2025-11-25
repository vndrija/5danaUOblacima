using System;

namespace API.DTOs;

public class CanteenSlotDto
{
    public string Date { get; set; } = string.Empty;
    public string Meal { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public int RemainingCapacity { get; set; }
}

using System;

namespace API.DTOs;

public class CanteenResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public List<WorkingHourDto> WorkingHours { get; set; } = new();
}

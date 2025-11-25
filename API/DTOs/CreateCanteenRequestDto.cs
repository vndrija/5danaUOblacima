using System;

namespace API.DTOs;

public class CreateCanteenRequestDto
{
    public required string Name { get; set; }
    public required string Location { get; set; }
    public int Capacity { get; set; }
    public List<WorkingHourDto> WorkingHours { get; set; } = new();
}

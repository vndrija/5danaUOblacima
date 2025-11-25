using System;

namespace API.DTOs;

public class UpdateCanteenRequestDto
{
    public string? Name { get; set; }
    public string? Location { get; set; }
    public int? Capacity { get; set; }
    public List<WorkingHourDto>? WorkingHours { get; set; }
}

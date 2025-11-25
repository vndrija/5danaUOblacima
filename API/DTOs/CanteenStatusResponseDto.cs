using System;

namespace API.DTOs;

public class CanteenStatusResponseDto
{
    public string CanteenId { get; set; } = string.Empty;
    public List<CanteenSlotDto> Slots { get; set; } = new();
}

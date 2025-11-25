using System;

namespace API.DTOs;

public class ReservationRequestDto
{
    public string StudentId { get; set; } = string.Empty;
    public string CanteenId { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public int Duration { get; set; }
}

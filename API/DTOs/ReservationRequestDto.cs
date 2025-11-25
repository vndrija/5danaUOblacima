using System;

namespace API.DTOs;

public class ReservationRequestDto
{
    public int StudentId { get; set; }
    public int CanteenId { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public int Duration { get; set; }
}

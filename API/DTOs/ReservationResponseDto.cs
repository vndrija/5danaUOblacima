using System;

namespace API.DTOs;

public class ReservationResponseDto
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public int StudentId { get; set; }
    public string CanteenId { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public int Duration { get; set; }
}

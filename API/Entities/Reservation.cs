using System;
using API.Enums;

namespace API.Entities;

public class Reservation
{
    public int Id { get; set; }
    public required int StudentId { get; set; }
    public required int CanteenId { get; set; }
    public required DateTime ReservationDate { get; set; }
    public required int Duration { get; set; } 

    public required TimeSpan Time { get; set; }
    public ReservationStatus Status { get; set;  } = ReservationStatus.Active;
    
    public Student Student { get; set; } = null!;
    public Canteen Canteen { get; set; } = null!;

}

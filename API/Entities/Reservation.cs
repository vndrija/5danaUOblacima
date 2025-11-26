using System;
using API.Enums;

namespace API.Entities;

public class Reservation
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CanteenId { get; set; }
    public DateOnly Date { get; set; }

    public string Time { get; set; } = string.Empty; // "12:00"

     public int Duration { get; set; } 
    
    public ReservationStatus Status { get; set;  } = ReservationStatus.Active;
    
    public Student Student { get; set; } = null!;
    public Canteen Canteen { get; set; } = null!;

}

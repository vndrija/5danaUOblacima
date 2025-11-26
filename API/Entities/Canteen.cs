using System;

namespace API.Entities;

public class Canteen
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Location { get; set; }
    public required int Capacity { get; set; }
    public List<WorkingHour> WorkingHours { get; set; } = new();
    public List<Reservation> Reservations { get; set; } = new();

}


using System;

namespace API.Entities;

public class Student
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public bool IsAdmin { get; set;} = false;

    public List<Reservation> Reservations { get; set; } = new();

}

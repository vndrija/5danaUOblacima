using System;
using API.Enums;

namespace API.Entities;

public class WorkingHour
{
    public int Id { get; set;}
    public int CanteenId { get; set; }
    public MealType Meal { get; set; }

    public string From { get; set; } = string.Empty; 
    public string To { get; set; } = string.Empty; 

    public Canteen Canteen { get; set; } = null!;
}

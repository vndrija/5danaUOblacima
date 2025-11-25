using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.Data;
using API.Entities;
using API.DTOs;
using API.Enums;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CanteensController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CanteensController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Canteens
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Canteen>>> GetCanteens()
        {
            return await _context.Canteens.ToListAsync();
        }

        // GET: api/Canteens/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Canteen>> GetCanteen(int id)
        {
            var canteen = await _context.Canteens.FindAsync(id);

            if (canteen == null)
            {
                return NotFound();
            }

            return canteen;
        }

        // PUT: api/Canteens/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCanteen(int id, Canteen canteen)
        {
            if (id != canteen.Id)
            {
                return BadRequest();
            }

            _context.Entry(canteen).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CanteenExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Canteens
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Canteen>> PostCanteen(CreateCanteenRequestDto canteen)
        {
            var newCanteen = new Canteen
    {
        Name = canteen.Name,
        Location = canteen.Location,
        Capacity = canteen.Capacity
    };

    // Add working hours
    foreach (var workingHourDto in canteen.WorkingHours)
    {
        var workingHour = new WorkingHour
        {
            Meal = ParseMealType(workingHourDto.Meal),
            From = workingHourDto.From,
            To = workingHourDto.To,
            Canteen = newCanteen
        };
        newCanteen.WorkingHours.Add(workingHour);
    }

    _context.Canteens.Add(newCanteen);
    await _context.SaveChangesAsync();

    // Map to response DTO (no circular references)
    var response = new CanteenResponseDto
    {
        Id = newCanteen.Id.ToString(),
        Name = newCanteen.Name,
        Location = newCanteen.Location,
        Capacity = newCanteen.Capacity,
        WorkingHours = newCanteen.WorkingHours.Select(wh => new WorkingHourDto
        {
            Meal = MealTypeToString(wh.Meal),
            From = wh.From,
            To = wh.To
        }).ToList()
    };

    return CreatedAtAction("GetCanteen", new { id = newCanteen.Id }, response);
}

// Helper to convert enum back to lowercase string
private string MealTypeToString(MealType mealType)
{
    return mealType switch
    {
        MealType.Breakfast => "breakfast",
        MealType.Lunch => "lunch",
        MealType.Dinner => "dinner",
        _ => throw new ArgumentException($"Unknown meal type: {mealType}")
    };
}

private MealType ParseMealType(string meal)
{
    return meal.ToLower() switch
    {
        "breakfast" => MealType.Breakfast,
        "lunch" => MealType.Lunch,
        "dinner" => MealType.Dinner,
        _ => throw new ArgumentException($"Invalid meal type: {meal}")
    };
}

        private bool CanteenExists(int id)
        {
            return _context.Canteens.Any(e => e.Id == id);
        }
    }
}

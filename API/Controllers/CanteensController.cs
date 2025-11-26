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
            var canteens = await _context.Canteens
                .Include(c => c.WorkingHours)
                .ToListAsync();

            var canteenDtos = canteens.Select(c => new CanteenResponseDto
            {
                Id = c.Id.ToString(),
                Name = c.Name,
                Location = c.Location,
                Capacity = c.Capacity,
                WorkingHours = c.WorkingHours.Select(wh => new WorkingHourDto
                {
                    Meal = MealTypeToString(wh.Meal),
                    From = wh.From,
                    To = wh.To
                }).ToList()
            });
            return Ok(canteenDtos);
        }

        // GET: api/Canteens/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Canteen>> GetCanteen(int id)
        {
            var canteen = await _context.Canteens
                .Include(c => c.WorkingHours)
                .FirstOrDefaultAsync(c => c.Id == id);



            if (canteen == null)
            {
                return NotFound();
            }

            var canteenDto = new CanteenResponseDto
            {
                Id = canteen.Id.ToString(),
                Name = canteen.Name,
                Location = canteen.Location,
                Capacity = canteen.Capacity,
                WorkingHours = canteen.WorkingHours.Select(wh => new WorkingHourDto
                {
                    Meal = MealTypeToString(wh.Meal),
                    From = wh.From,
                    To = wh.To
                }).ToList()
            };

            return Ok(canteenDto);
        }

        [HttpGet("status")]
        public async Task<ActionResult<IEnumerable<CanteenStatusResponseDto>>> GetCanteensStatus(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] TimeSpan startTime,
        [FromQuery] TimeSpan endTime,
        [FromQuery] int duration)
        {
            if (startDate > endDate || duration <= 0)
                return BadRequest("Invalid input parameters.");

            var canteens = await _context.Canteens.Include(c => c.WorkingHours).ToListAsync();
            var response = new List<CanteenStatusResponseDto>();

            foreach (var canteen in canteens)
            {
                var slots = new List<CanteenSlotDto>();

                foreach (var workingHour in canteen.WorkingHours)
                {
                    var whStart = TimeSpan.Parse(workingHour.From);
                    var whEnd = TimeSpan.Parse(workingHour.To);

                    var slotStart = (whStart > startTime) ? whStart : startTime;
                    var slotEnd = (whEnd < endTime) ? whEnd : endTime;

                    if (slotStart >= slotEnd) continue;

                    for (var date = startDate; date <= endDate; date = date.AddDays(1))
                    {
                        var currentTime = slotStart;

                        while (currentTime.Add(TimeSpan.FromMinutes(duration)) <= slotEnd)
                        {
                            var reservations = await _context.Reservations
                                .Where(r => r.CanteenId == canteen.Id &&
                                            r.ReservationDate == date &&
                                            r.Time == currentTime &&
                                            r.Status == ReservationStatus.Active)
                                .CountAsync();

                            var remainingCapacity = canteen.Capacity - reservations;

                            slots.Add(new CanteenSlotDto
                            {
                                Date = date.ToString("yyyy-MM-dd"),
                                Meal = workingHour.Meal.ToString().ToLower(),
                                StartTime = currentTime.ToString(@"hh\:mm"),
                                RemainingCapacity = remainingCapacity
                            });

                            currentTime = currentTime.Add(TimeSpan.FromMinutes(duration));
                        }
                    }
                }

                response.Add(new CanteenStatusResponseDto
                {
                    CanteenId = canteen.Id.ToString(),
                    Slots = slots
                });
            }

            return Ok(response);
        }

        [HttpGet("{id}/status")]
        public async Task<ActionResult<CanteenStatusResponseDto>> GetCanteenStatus(
        int id,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] TimeSpan startTime,
        [FromQuery] TimeSpan endTime,
        [FromQuery] int duration)
        {
            if (startDate > endDate || duration <= 0)
                return BadRequest("Invalid input parameters.");

            var canteen = await _context.Canteens
                .Include(c => c.WorkingHours)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (canteen == null)
                return NotFound();

            var slots = new List<CanteenSlotDto>();

            foreach (var workingHour in canteen.WorkingHours)
            {
                var whStart = TimeSpan.Parse(workingHour.From);
                var whEnd = TimeSpan.Parse(workingHour.To);

                var slotStart = (whStart > startTime) ? whStart : startTime;
                var slotEnd = (whEnd < endTime) ? whEnd : endTime;

                if (slotStart >= slotEnd) continue;

                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    var currentTime = slotStart;

                    while (currentTime.Add(TimeSpan.FromMinutes(duration)) <= slotEnd)
                    {
                        var reservations = await _context.Reservations
                            .Where(r => r.CanteenId == canteen.Id &&
                                        r.ReservationDate == date &&
                                        r.Time == currentTime &&
                                        r.Status == ReservationStatus.Active)
                            .CountAsync();

                        slots.Add(new CanteenSlotDto
                        {
                            Date = date.ToString("yyyy-MM-dd"),
                            Meal = workingHour.Meal.ToString().ToLower(),
                            StartTime = currentTime.ToString(@"hh\:mm"),

                            RemainingCapacity = canteen.Capacity - reservations
                        });

                        currentTime = currentTime.Add(TimeSpan.FromMinutes(duration));
                    }
                }
            }

            var response = new CanteenStatusResponseDto
            {
                CanteenId = canteen.Id.ToString(),
                Slots = slots
            };

            return Ok(response);
        }
        // PUT: api/Canteens/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCanteen(int id, UpdateCanteenRequestDto canteenDto, [FromHeader] int studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null || !student.IsAdmin)
            {
                return StatusCode(403, "Samo redar moze napraviti menzu.");
            }

            var canteen = await _context.Canteens
            .Include(c => c.WorkingHours) 
            .FirstOrDefaultAsync(c => c.Id == id);
            if (canteen == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(canteenDto.Name))
                canteen.Name = canteenDto.Name;

            if (!string.IsNullOrEmpty(canteenDto.Location))
                canteen.Location = canteenDto.Location;

            if (canteenDto.Capacity != null)
                canteen.Capacity = canteenDto.Capacity.Value;

            await _context.SaveChangesAsync();

            var response = new CanteenResponseDto
            {
                Id = canteen.Id.ToString(),
                Name = canteen.Name,
                Location = canteen.Location,
                Capacity = canteen.Capacity,
                WorkingHours = canteen.WorkingHours.Select(wh => new WorkingHourDto
                {
                    Meal = MealTypeToString(wh.Meal),
                    From = wh.From,
                    To = wh.To
                }).ToList()
            };

            return Ok(response);
        }

        // POST: api/Canteens
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Canteen>> PostCanteen(CreateCanteenRequestDto canteen, [FromHeader] int studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null || !student.IsAdmin)
            {
                return StatusCode(403, "Samo redar moze napraviti menzu.");
            }

            var newCanteen = new Canteen
            {
                Name = canteen.Name,
                Location = canteen.Location,
                Capacity = canteen.Capacity
            };

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
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCanteen(int id, [FromHeader] string studentId)
        {
            var student = await _context.Students.FindAsync(int.Parse(studentId));
            if (student == null || !student.IsAdmin)
            {
                return Forbid("Only admin students can delete a canteen.");
            }
            {

                var canteen = await _context.Canteens.FindAsync(id);

                if (canteen == null)
                {
                    return NotFound();
                }

                _context.Canteens.Remove(canteen);
                await _context.SaveChangesAsync();

                return NoContent();
            }
        }

        private bool CanteenExists(int id)
        {
            return _context.Canteens.Any(e => e.Id == id);
        }
    }
}

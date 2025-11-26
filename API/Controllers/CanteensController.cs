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
using AutoMapper;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CanteensController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public CanteensController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/Canteens
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Canteen>>> GetCanteens()
        {
            var canteens = await _context.Canteens
                .Include(c => c.WorkingHours)
                .ToListAsync();

            var canteenDtos = _mapper.Map<List<CanteenResponseDto>>(canteens);
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

            var canteenDto = _mapper.Map<CanteenResponseDto>(canteen);

            return Ok(canteenDto);
        }

        // PUT: api/Canteens/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCanteen(int id, UpdateCanteenRequestDto canteenDto, [FromHeader] int studentId)
        {
            if (canteenDto == null)
                return BadRequest("Podaci za azuriranje nisu poslati.");

            var student = await _context.Students.FindAsync(studentId);

            if (student == null || !student.IsAdmin)
                return StatusCode(403, "Samo redar moze azurirati menzu.");

            var canteen = await _context.Canteens
                .Include(c => c.WorkingHours)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (canteen == null)
                return NotFound();

            _mapper.Map(canteenDto, canteen);

            await _context.SaveChangesAsync();

            var response = _mapper.Map<CanteenResponseDto>(canteen);

            return Ok(response);
        }

        // POST: api/Canteens
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<CanteenResponseDto>> PostCanteen(
        CreateCanteenRequestDto canteenDto,
        [FromHeader] int studentId)
        {
            var student = await _context.Students.FindAsync(studentId);

            if (student == null || !student.IsAdmin)
                return StatusCode(403, "Samo redar moze napraviti menzu.");

            if (canteenDto.WorkingHours == null || !canteenDto.WorkingHours.Any())
                return BadRequest("Menza mora imati radno vreme.");

            var newCanteen = _mapper.Map<Canteen>(canteenDto);

            _context.Canteens.Add(newCanteen);
            await _context.SaveChangesAsync();

            var response = _mapper.Map<CanteenResponseDto>(newCanteen);

            return CreatedAtAction(nameof(GetCanteen), new { id = newCanteen.Id }, response);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCanteen(int id, [FromHeader] int studentId)
        {
            var student = await _context.Students.FindAsync(studentId);

            if (student == null || !student.IsAdmin)
            {
                return StatusCode(403, "Samo redar moze obrisati menzu.");
            }



            var canteen = await _context.Canteens.FindAsync(id);

            if (canteen == null)
            {
                return NotFound();
            }

            _context.Canteens.Remove(canteen);
            await _context.SaveChangesAsync();

            return NoContent();

        }

        [HttpGet("status")]
        public async Task<ActionResult<IEnumerable<CanteenStatusResponseDto>>> GetCanteensStatus(
        [FromQuery] string startDate,
        [FromQuery] string endDate,
        [FromQuery] string startTime,
        [FromQuery] string endTime,
        [FromQuery] int duration)
        {
            if (!DateOnly.TryParse(startDate, out var start) ||
                !DateOnly.TryParse(endDate, out var end) ||
                !TimeOnly.TryParse(startTime, out var timeStart) ||
                !TimeOnly.TryParse(endTime, out var timeEnd))
            {
                return BadRequest("Nevalidan format datuma ili vremena.");
            }

            if (start > end || duration <= 0 || (duration != 30 && duration != 60))
            {
                return BadRequest("Nevalidni ulazni parametri.");
            }

            var canteens = await _context.Canteens
                .Include(c => c.WorkingHours)
                .ToListAsync();

            var response = new List<CanteenStatusResponseDto>();

            foreach (var canteen in canteens)
            {
                var slots = new List<CanteenSlotDto>();

                foreach (var workingHour in canteen.WorkingHours)
                {
                    var whStart = TimeOnly.Parse(workingHour.From);
                    var whEnd = TimeOnly.Parse(workingHour.To);

                    var slotStart = (whStart > timeStart) ? whStart : timeStart;
                    var slotEnd = (whEnd < timeEnd) ? whEnd : timeEnd;

                    if (slotStart >= slotEnd) continue;

                    for (var date = start; date <= end; date = date.AddDays(1))
                    {
                        var currentTime = slotStart;

                        while (currentTime.AddMinutes(duration) <= slotEnd)
                        {
                            var currentTimeStr = currentTime.ToString("HH:mm");

                            var reservations = await _context.Reservations
                                .Where(r => r.CanteenId == canteen.Id &&
                                            r.Date == date &&
                                            r.Status == ReservationStatus.Active)
                                .ToListAsync();

                            var overlappingCount = reservations.Count(r =>
                            {
                                var resStart = TimeOnly.Parse(r.Time);
                                var resEnd = resStart.AddMinutes(r.Duration);
                                var slotEnd = currentTime.AddMinutes(duration);

                                return resStart < slotEnd && resEnd > currentTime;
                            });

                            var remainingCapacity = canteen.Capacity - overlappingCount;

                            slots.Add(new CanteenSlotDto
                            {
                                Date = date.ToString("yyyy-MM-dd"),
                                Meal = workingHour.Meal.ToString().ToLower(),
                                StartTime = currentTimeStr,
                                RemainingCapacity = remainingCapacity
                            });

                            currentTime = currentTime.AddMinutes(duration);
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
        [FromQuery] string startDate,
        [FromQuery] string endDate,
        [FromQuery] string startTime,
        [FromQuery] string endTime,
        [FromQuery] int duration)
        {
            if (!DateOnly.TryParse(startDate, out var start) ||
                !DateOnly.TryParse(endDate, out var end) ||
                !TimeOnly.TryParse(startTime, out var timeStart) ||
                !TimeOnly.TryParse(endTime, out var timeEnd))
            {
                return BadRequest("Nevalidan format datuma ili vremena.");
            }

            if (start > end || duration <= 0 || (duration != 30 && duration != 60))
            {
                return BadRequest("Nevalidni ulazni parametri.");
            }

            var canteen = await _context.Canteens
                .Include(c => c.WorkingHours)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (canteen == null)
                return NotFound();

            var slots = new List<CanteenSlotDto>();

            foreach (var workingHour in canteen.WorkingHours)
            {
                var whStart = TimeOnly.Parse(workingHour.From);
                var whEnd = TimeOnly.Parse(workingHour.To);

                var slotStart = (whStart > timeStart) ? whStart : timeStart;
                var slotEnd = (whEnd < timeEnd) ? whEnd : timeEnd;

                if (slotStart >= slotEnd) continue;

                for (var date = start; date <= end; date = date.AddDays(1))
                {
                    var currentTime = slotStart;

                    while (currentTime.AddMinutes(duration) <= slotEnd)
                    {
                        var currentTimeStr = currentTime.ToString("HH:mm");

                        var reservations = await _context.Reservations
                            .Where(r => r.CanteenId == canteen.Id &&
                                        r.Date == date &&
                                        r.Status == ReservationStatus.Active)
                            .ToListAsync();

                        var overlappingCount = reservations.Count(r =>
                        {
                            var resStart = TimeOnly.Parse(r.Time);
                            var resEnd = resStart.AddMinutes(r.Duration);
                            var slotEnd = currentTime.AddMinutes(duration);

                            return resStart < slotEnd && resEnd > currentTime;
                        });

                        slots.Add(new CanteenSlotDto
                        {
                            Date = date.ToString("yyyy-MM-dd"),
                            Meal = workingHour.Meal.ToString().ToLower(),
                            StartTime = currentTimeStr,
                            RemainingCapacity = canteen.Capacity - overlappingCount
                        });

                        currentTime = currentTime.AddMinutes(duration);
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
    }
}

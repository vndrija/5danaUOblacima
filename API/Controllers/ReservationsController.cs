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
using AutoMapper;
using API.Enums;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public ReservationsController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/Reservations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReservationResponseDto>>> GetReservations()
        {
            var reservations = await _context.Reservations.ToListAsync();
            var reservationDtos = _mapper.Map<List<ReservationResponseDto>>(reservations);
            return reservationDtos;
        }

        // GET: api/Reservations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ReservationResponseDto>> GetReservation(int id)
        {

            var reservation = await _context.Reservations
            .FindAsync(id);

            if (reservation == null)
            {
                return NotFound();
            }

            var responseDto = _mapper.Map<ReservationResponseDto>(reservation);


            return Ok(responseDto);
        }

        // PUT: api/Reservations/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutReservation(int id, ReservationRequestDto reservationDto)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }
            _mapper.Map(reservationDto, reservation);

            _context.Entry(reservation).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReservationExists(id))
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

        // POST: api/Reservations
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ReservationResponseDto>> PostReservation(ReservationRequestDto reservationDto)
        {
            try
            {
                if (!int.TryParse(reservationDto.StudentId, out var studentId) ||
                    !int.TryParse(reservationDto.CanteenId, out var canteenId))
                {
                    return BadRequest(new { error = "Invalid student or canteen ID" });
                }

                if (!DateOnly.TryParse(reservationDto.Date, out var date))
                {
                    return BadRequest(new { error = "Invalid date format" });
                }

                if (date < DateOnly.FromDateTime(DateTime.Today))
                {
                    return BadRequest(new { error = "Reservation date cannot be in the past" });
                }

                // Parse and validate time
                if (!TimeOnly.TryParse(reservationDto.Time, out var timeOnly))
                {
                    return BadRequest(new { error = "Invalid time format" });
                }

                // Validate time is on hour or half-hour
                if (timeOnly.Minute != 0 && timeOnly.Minute != 30)
                {
                    return BadRequest(new { error = "Time must start on the hour or half-hour" });
                }

                // Validate duration
                if (reservationDto.Duration != 30 && reservationDto.Duration != 60)
                {
                    return BadRequest(new { error = "Duration must be 30 or 60 minutes" });
                }

                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                {
                    return BadRequest(new { error = "Student does not exist" });
                }

                // Check if canteen exists and get working hours
                var canteen = await _context.Canteens
                    .Include(c => c.WorkingHours)
                    .FirstOrDefaultAsync(c => c.Id == canteenId);

                if (canteen == null)
                {
                    return BadRequest(new { error = "Canteen does not exist" });
                }

                var reservationEnd = timeOnly.AddMinutes(reservationDto.Duration);

                var isWithinWorkingHours = canteen.WorkingHours.Any(wh =>
                {
                    var whStart = TimeOnly.Parse(wh.From);
                    var whEnd = TimeOnly.Parse(wh.To);

                    return timeOnly >= whStart && reservationEnd <= whEnd;
                });

                if (!isWithinWorkingHours)
                {
                    return BadRequest(new { error = "Reservation time is outside the canteen's working hours" });
                }

                var existingReservations = await _context.Reservations
                    .Where(r => r.StudentId == studentId &&
                                r.Date == date &&
                                r.Status == ReservationStatus.Active)
                    .ToListAsync();

                var hasOverlap = existingReservations.Any(existing =>
                {
                    var existingStart = TimeOnly.Parse(existing.Time);
                    var existingEnd = existingStart.AddMinutes(existing.Duration);

                    return existingStart < reservationEnd && existingEnd > timeOnly;
                });

                if (hasOverlap)
                {
                    return BadRequest(new { error = "Student already has a reservation at this time" });
                }

                var activeReservations = await _context.Reservations
                    .Where(r => r.CanteenId == canteenId &&
                                r.Date == date &&
                                r.Status == ReservationStatus.Active)
                    .ToListAsync();

                var overlappingCount = activeReservations.Count(r =>
                {
                    var resStart = TimeOnly.Parse(r.Time);
                    var resEnd = resStart.AddMinutes(r.Duration);

                    return resStart < reservationEnd && resEnd > timeOnly;
                });

                if (overlappingCount >= canteen.Capacity)
                {
                    return BadRequest(new { error = "Canteen is at full capacity for this time slot" });
                }

                var reservation = _mapper.Map<Reservation>(reservationDto);

                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();

                // Map to response
                var response = _mapper.Map<ReservationResponseDto>(reservation);

                return CreatedAtAction(nameof(GetReservation), new { id = reservation.Id }, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", details = ex.Message });
            }
        }


        // DELETE: api/Reservations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReservation(int id, [FromHeader] int studentId)
        {

            var reservation = await _context.Reservations.FindAsync(id);

            if (reservation == null)
            {
                return NotFound();
            }

            if (reservation.StudentId != studentId)
            {
                return StatusCode(403, "Only the student who made the reservation can cancel it.");
            }

            if (reservation.Status == ReservationStatus.Cancelled)
            {
                return BadRequest("Reservation is already cancelled.");
            }

            reservation.Status = ReservationStatus.Cancelled;
            _context.Entry(reservation).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while cancelling the reservation.");
            }

            var response = _mapper.Map<ReservationResponseDto>(reservation);


            return Ok(response);
        }

        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(e => e.Id == id);
        }
    }
}

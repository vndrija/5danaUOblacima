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

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReservationsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Reservations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Reservation>>> GetReservations()
        {
            return await _context.Reservations.ToListAsync();
        }

        // GET: api/Reservations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ReservationResponseDto>> GetReservation(int id)
        {

            var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            var response = new ReservationResponseDto
            {
                Id = reservation.Id,
                StudentId = reservation.StudentId,
                CanteenId = reservation.CanteenId.ToString(),
                Date = reservation.ReservationDate.ToString("yyyy-MM-dd"),
                Time = reservation.Time.ToString(@"hh\:mm"),
                Duration = reservation.Duration,
                Status = reservation.Status.ToString()
            };


            return response;
        }

        // PUT: api/Reservations/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutReservation(int id, Reservation reservation)
        {
            if (id != reservation.Id)
            {
                return BadRequest();
            }

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
            var reservation = new Reservation
            {
                StudentId = reservationDto.StudentId,
                CanteenId = reservationDto.CanteenId,
                ReservationDate = DateTime.Parse(reservationDto.Date),
                Time = TimeSpan.Parse(reservationDto.Time),
                Duration = reservationDto.Duration
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            var response = new ReservationResponseDto
            {
                Id = reservation.Id,
                StudentId = reservation.StudentId,
                CanteenId = reservation.CanteenId.ToString(),
                Date = reservation.ReservationDate.ToString("yyyy-MM-dd"),
                Time = reservation.Time.ToString(@"hh\:mm"),
                Duration = reservation.Duration,
                Status = reservation.Status.ToString()
            };

            return CreatedAtAction(nameof(GetReservation), new { id = reservation.Id }, response);
        }

        // DELETE: api/Reservations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReservation(int id, [FromHeader] string studentId)
        {
            var student = await _context.Students.FindAsync(int.Parse(studentId));
            if (student == null)
            {
                return BadRequest("Invalid student ID.");
            }

            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }

            // Ensure the reservation belongs to the student
            if (reservation.StudentId.ToString() != studentId)
            {
                return Forbid("You can only cancel your own reservations.");
            }

            // Update the reservation status to "Cancelled"
            reservation.Status = Enums.ReservationStatus.Cancelled;
            _context.Entry(reservation).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while canceling the reservation.");
            }

            // Return the updated reservation as a response
            var response = new ReservationResponseDto
            {
                Id = reservation.Id,
                StudentId = reservation.StudentId,
                CanteenId = reservation.CanteenId.ToString(),
                Date = reservation.ReservationDate.ToString("yyyy-MM-dd"),
                Time = reservation.Time.ToString(@"hh\:mm"),
                Duration = reservation.Duration,
                Status = reservation.Status.ToString()
            };

            return Ok(response);
        }

        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(e => e.Id == id);
        }
    }
}

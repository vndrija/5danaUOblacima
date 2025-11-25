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
                Id = reservation.Id.ToString(),
                StudentId = reservation.StudentId.ToString(),
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
                StudentId = int.Parse(reservationDto.StudentId),
                CanteenId = int.Parse(reservationDto.CanteenId),
                ReservationDate = DateTime.Parse(reservationDto.Date),
                Time = TimeSpan.Parse(reservationDto.Time),
                Duration = reservationDto.Duration
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            var response = new ReservationResponseDto
            {
                Id = reservation.Id.ToString(),
                StudentId = reservation.StudentId.ToString(),
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
            if (student == null || !student.IsAdmin)
            {
                return Forbid("Only admin students can delete a canteen.");
            }

            // Find the canteen
            var canteen = await _context.Canteens.FindAsync(id);
            if (canteen == null)
            {
                return NotFound();
            }

            // Remove the canteen
            _context.Canteens.Remove(canteen);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(e => e.Id == id);
        }
    }
}

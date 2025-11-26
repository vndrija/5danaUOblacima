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
            var reservationDate = DateOnly.Parse(reservationDto.Date);
            if (reservationDate < DateOnly.FromDateTime(DateTime.Now))
            {
                return BadRequest("Rezervacija mora biti u buducnosti.");
            }


            var reservation = _mapper.Map<Reservation>(reservationDto);

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            var response = _mapper.Map<ReservationResponseDto>(reservation);

            return CreatedAtAction(nameof(GetReservation), new { id = reservation.Id }, response);
        }

        // DELETE: api/Reservations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReservation(int id, [FromHeader] int studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
            {
                return BadRequest("Nevalidan studentId.");
            }

            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }

            if (reservation.StudentId != studentId)
            {
                return StatusCode(403, "Samo student koji je napravio rezervaciju moze da je otkaze.");
            }

            reservation.Status = ReservationStatus.Cancelled;
            _context.Entry(reservation).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                return StatusCode(500, "Greška prilikom otkazivanja rezervacije.");
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

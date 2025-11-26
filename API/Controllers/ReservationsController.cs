using Microsoft.AspNetCore.Mvc;
using API.DTOs;
using API.Services;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationService _reservationService;

        public ReservationsController(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        // GET: api/Reservations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReservationResponseDto>>> GetReservations()
        {
            var reservations = await _reservationService.GetAllReservationsAsync();
            return Ok(reservations);
        }

        // GET: api/Reservations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ReservationResponseDto>> GetReservation(int id)
        {
            var reservation = await _reservationService.GetReservationByIdAsync(id);

            if (reservation == null)
            {
                return NotFound();
            }

            return Ok(reservation);
        }

        // PUT: api/Reservations/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutReservation(int id, ReservationRequestDto reservationDto)
        {
            var result = await _reservationService.UpdateReservationAsync(id, reservationDto);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }

        // POST: api/Reservations
        [HttpPost]
        public async Task<ActionResult<ReservationResponseDto>> PostReservation(ReservationRequestDto reservationDto)
        {
            var response = await _reservationService.CreateReservationAsync(reservationDto);
            return CreatedAtAction(nameof(GetReservation), new { id = int.Parse(response.Id) }, response);
        }


        // DELETE: api/Reservations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReservation(int id, [FromHeader] int studentId)
        {
            var response = await _reservationService.CancelReservationAsync(id, studentId);

            if (response == null)
            {
                return NotFound();
            }

            return Ok(response);
        }
    }
}

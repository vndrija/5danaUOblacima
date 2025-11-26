using Microsoft.AspNetCore.Mvc;
using API.DTOs;
using API.Services;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CanteensController : ControllerBase
    {
        private readonly ICanteenService _canteenService;

        public CanteensController(ICanteenService canteenService)
        {
            _canteenService = canteenService;
        }

        // GET: api/Canteens
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CanteenResponseDto>>> GetCanteens()
        {
            var canteens = await _canteenService.GetAllCanteensAsync();
            return Ok(canteens);
        }

        // GET: api/Canteens/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CanteenResponseDto>> GetCanteen(int id)
        {
            var canteen = await _canteenService.GetCanteenByIdAsync(id);

            if (canteen == null)
            {
                return NotFound();
            }

            return Ok(canteen);
        }

        // PUT: api/Canteens/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCanteen(int id, UpdateCanteenRequestDto canteenDto, [FromHeader] int studentId)
        {
            var response = await _canteenService.UpdateCanteenAsync(id, canteenDto, studentId);

            if (response == null)
            {
                return NotFound();
            }

            return Ok(response);
        }

        // POST: api/Canteens
        [HttpPost]
        public async Task<ActionResult<CanteenResponseDto>> PostCanteen(
        CreateCanteenRequestDto canteenDto,
        [FromHeader] int studentId)
        {
            var response = await _canteenService.CreateCanteenAsync(canteenDto, studentId);
            return CreatedAtAction(nameof(GetCanteen), new { id = int.Parse(response.Id) }, response);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCanteen(int id, [FromHeader] int studentId)
        {
            var result = await _canteenService.DeleteCanteenAsync(id, studentId);

            if (!result)
            {
                return NotFound();
            }

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
            var response = await _canteenService.GetCanteensStatusAsync(startDate, endDate, startTime, endTime, duration);
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
            var response = await _canteenService.GetCanteenStatusAsync(id, startDate, endDate, startTime, endTime, duration);

            if (response == null)
            {
                return NotFound();
            }

            return Ok(response);
        }
    }
}

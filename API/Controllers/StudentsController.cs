using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.Data;
using API.Entities;
using API.DTOs;
using AutoMapper;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public StudentsController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/Students
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudentResponseDto>>> GetStudents()
        {
            var students = await _context.Students.ToListAsync();
            var studentDtos = _mapper.Map<List<StudentResponseDto>>(students);
            return studentDtos;
        }

        // GET: api/Students/5
        [HttpGet("{id}")]
        public async Task<ActionResult<StudentResponseDto>> GetStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);

            if (student == null)
            {
                return NotFound();
            }
            var studentDto = _mapper.Map<StudentResponseDto>(student);

            return Ok(studentDto);
        }

        // PUT: api/Students/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutStudent(int id, StudentRequestDto studentDto)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            var student = await _context.Students.FindAsync(id);
            
            if (student == null)
            {
                return NotFound();
            }

            _mapper.Map(studentDto, student);

            _context.Entry(student).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StudentExists(id))
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

        // POST: api/Students
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<StudentResponseDto>> PostStudent(StudentRequestDto studentDto)
        {
            var student = _mapper.Map<Student>(studentDto);

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            var studentResponseDto = _mapper.Map<StudentResponseDto>(student);

            return CreatedAtAction(nameof(GetStudent), new { id = student.Id }, studentResponseDto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Id == id);
        }
    }
}

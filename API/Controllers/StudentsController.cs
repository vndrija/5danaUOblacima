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
    public class StudentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StudentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Students
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudentResponseDto>>> GetStudents()
        {
            var students = await _context.Students.ToListAsync();
            var studentDtos = students.Select(student => new StudentResponseDto
            {
                Id = student.Id,
                Name = student.Name,
                Email = student.Email,
                IsAdmin = student.IsAdmin
            }).ToList();

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
            var studentDto = new StudentResponseDto
            {
                Id = student.Id,
                Name = student.Name,
                Email = student.Email,
                IsAdmin = student.IsAdmin
            };

            return Ok(studentDto);
        }

        // PUT: api/Students/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutStudent(int id, Student student)
        {
            if (id != student.Id)
            {
                return BadRequest();
            }

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
            var student = new Student
            {
                Name = studentDto.Name,
                Email = studentDto.Email,
                IsAdmin = studentDto.IsAdmin
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            var studentResponseDto = new StudentResponseDto
            {
                Id = student.Id,
                Name = student.Name,
                Email = student.Email,
                IsAdmin = student.IsAdmin
            };

            return CreatedAtAction(nameof(GetStudent), new { id = student.Id }, studentResponseDto);
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Id == id);
        }
    }
}

using API.DTOs;
using API.Entities;
using API.Repositories;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace API.Services
{
    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _repository;
        private readonly IMapper _mapper;

        public StudentService(IStudentRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<StudentResponseDto>> GetAllStudentsAsync()
        {
            var students = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<StudentResponseDto>>(students);
        }

        public async Task<StudentResponseDto?> GetStudentByIdAsync(int id)
        {
            var student = await _repository.GetByIdAsync(id);
            if (student == null)
            {
                return null;
            }
            return _mapper.Map<StudentResponseDto>(student);
        }

        public async Task<StudentResponseDto> CreateStudentAsync(StudentRequestDto studentDto)
        {
            if (string.IsNullOrWhiteSpace(studentDto.Email) || !studentDto.Email.Contains("@"))
            {
                throw new ArgumentException("Invalid email format");
            }

            var existingStudent = await _repository.GetByEmailAsync(studentDto.Email);
            if (existingStudent != null)
            {
                throw new InvalidOperationException("A student with this email already exists");
            }

            var student = _mapper.Map<Student>(studentDto);
            var createdStudent = await _repository.AddAsync(student);

            return _mapper.Map<StudentResponseDto>(createdStudent);
        }

        public async Task<bool> UpdateStudentAsync(int id, StudentRequestDto studentDto)
        {
            if (id <= 0)
            {
                return false;
            }

            var student = await _repository.GetByIdAsync(id);
            if (student == null)
            {
                return false;
            }

            _mapper.Map(studentDto, student);

            try
            {
                await _repository.UpdateAsync(student);
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _repository.ExistsAsync(id))
                {
                    return false;
                }
                throw;
            }
        }

        public async Task<bool> DeleteStudentAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<bool> StudentExistsAsync(int id)
        {
            return await _repository.ExistsAsync(id);
        }
    }
}

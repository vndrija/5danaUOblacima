using API.Controllers;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Exceptions;
using API.Repositories;
using API.Services;
using CanteenReservationSystem.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanteenReservationSystem.Tests.Controllers;

public class StudentsControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly StudentsController _controller;
    private readonly IStudentService _studentService;

    public StudentsControllerTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext($"StudentsTestDb_{Guid.NewGuid()}");
        var mapper = MapperHelper.CreateMapper();
        var repository = new StudentRepository(_context);
        _studentService = new StudentService(repository, mapper);
        _controller = new StudentsController(_studentService);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task PostStudent_ReturnsCreatedStudent_WhenValidDataProvided()
    {
        // Arrange
        var studentDto = new StudentRequestDto
        {
            Name = "John Doe",
            Email = "john@example.com",
            IsAdmin = false
        };

        // Act
        var result = await _controller.PostStudent(studentDto);

        // Assert
        var actionResult = Assert.IsType<ActionResult<StudentResponseDto>>(result);
        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        var createdStudent = Assert.IsType<StudentResponseDto>(createdResult.Value);
        Assert.Equal("John Doe", createdStudent.Name);
        Assert.Equal("john@example.com", createdStudent.Email);

        // Verify student was saved to database
        var savedStudent = await _context.Students.FirstOrDefaultAsync(s => s.Email == "john@example.com");
        Assert.NotNull(savedStudent);
    }

    [Fact]
    public async Task PostStudent_ReturnsBadRequest_WhenEmailAlreadyExists()
    {
        // Arrange
        var existingStudent = new Student { Name = "Existing Student", Email = "john@example.com", IsAdmin = false };
        _context.Students.Add(existingStudent);
        await _context.SaveChangesAsync();

        var studentDto = new StudentRequestDto
        {
            Name = "New Student",
            Email = "john@example.com",
            IsAdmin = false
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(async () =>
        {
            await _controller.PostStudent(studentDto);
        });

        Assert.Equal("A student with this email already exists", exception.Message);
    }

    [Fact]
    public async Task GetStudent_ReturnsStudent_WhenStudentExists()
    {
        // Arrange
        var student = new Student { Name = "John Doe", Email = "john@example.com", IsAdmin = false };
        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetStudent(student.Id);

        // Assert
        var actionResult = Assert.IsType<ActionResult<StudentResponseDto>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returnedStudent = Assert.IsType<StudentResponseDto>(okResult.Value);
        Assert.Equal(student.Id.ToString(), returnedStudent.Id);
        Assert.Equal("John Doe", returnedStudent.Name);
        Assert.Equal("john@example.com", returnedStudent.Email);
    }

    [Fact]
    public async Task GetStudent_ReturnsNotFound_WhenStudentDoesNotExist()
    {
        // Act
        var result = await _controller.GetStudent(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }
}

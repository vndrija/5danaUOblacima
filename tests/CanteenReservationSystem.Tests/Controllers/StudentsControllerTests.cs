using API.Controllers;
using API.Data;
using API.DTOs;
using API.Entities;
using CanteenReservationSystem.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanteenReservationSystem.Tests.Controllers;

public class StudentsControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly StudentsController _controller;

    public StudentsControllerTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext($"StudentsTestDb_{Guid.NewGuid()}");
        var mapper = MapperHelper.CreateMapper();
        _controller = new StudentsController(_context, mapper);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetStudents_ReturnsEmptyList_WhenNoStudentsExist()
    {
        // Act
        var result = await _controller.GetStudents();

        // Assert
        var actionResult = Assert.IsType<ActionResult<IEnumerable<StudentResponseDto>>>(result);
        var studentList = Assert.IsAssignableFrom<IEnumerable<StudentResponseDto>>(actionResult.Value);
        Assert.Empty(studentList);
    }

    [Fact]
    public async Task GetStudents_ReturnsAllStudents_WhenStudentsExist()
    {
        // Arrange
        var students = new List<Student>
        {
            new() { Name = "John Doe", Email = "john@example.com", IsAdmin = false },
            new() { Name = "Jane Smith", Email = "jane@example.com", IsAdmin = true }
        };
        _context.Students.AddRange(students);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetStudents();

        // Assert
        var actionResult = Assert.IsType<ActionResult<IEnumerable<StudentResponseDto>>>(result);
        var studentList = Assert.IsAssignableFrom<IEnumerable<StudentResponseDto>>(actionResult.Value).ToList();
        Assert.Equal(2, studentList.Count);
        Assert.Contains(studentList, s => s.Email == "john@example.com");
        Assert.Contains(studentList, s => s.Email == "jane@example.com");
    }

    [Fact]
    public async Task GetStudent_ReturnsNotFound_WhenStudentDoesNotExist()
    {
        // Act
        var result = await _controller.GetStudent(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
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
    public async Task PostStudent_ReturnsBadRequest_WhenEmailIsNull()
    {
        // Arrange
        var studentDto = new StudentRequestDto
        {
            Name = "John Doe",
            Email = null!,
            IsAdmin = false
        };

        // Act
        var result = await _controller.PostStudent(studentDto);

        // Assert
        var actionResult = Assert.IsType<ActionResult<StudentResponseDto>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task PostStudent_ReturnsBadRequest_WhenEmailIsWhitespace()
    {
        // Arrange
        var studentDto = new StudentRequestDto
        {
            Name = "John Doe",
            Email = "   ",
            IsAdmin = false
        };

        // Act
        var result = await _controller.PostStudent(studentDto);

        // Assert
        var actionResult = Assert.IsType<ActionResult<StudentResponseDto>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task PostStudent_ReturnsBadRequest_WhenEmailDoesNotContainAtSymbol()
    {
        // Arrange
        var studentDto = new StudentRequestDto
        {
            Name = "John Doe",
            Email = "johnexample.com",
            IsAdmin = false
        };

        // Act
        var result = await _controller.PostStudent(studentDto);

        // Assert
        var actionResult = Assert.IsType<ActionResult<StudentResponseDto>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.NotNull(badRequestResult.Value);
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

        // Act
        var result = await _controller.PostStudent(studentDto);

        // Assert
        var actionResult = Assert.IsType<ActionResult<StudentResponseDto>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task PutStudent_ReturnsNoContent_WhenUpdateSuccessful()
    {
        // Arrange
        var student = new Student { Name = "John Doe", Email = "john@example.com", IsAdmin = false };
        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        var updateDto = new StudentRequestDto
        {
            Name = "John Updated",
            Email = "john.updated@example.com",
            IsAdmin = true
        };

        // Act
        var result = await _controller.PutStudent(student.Id, updateDto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify update was saved
        var updatedStudent = await _context.Students.FindAsync(student.Id);
        Assert.NotNull(updatedStudent);
        Assert.Equal("John Updated", updatedStudent.Name);
        Assert.Equal("john.updated@example.com", updatedStudent.Email);
        Assert.True(updatedStudent.IsAdmin);
    }

    [Fact]
    public async Task PutStudent_ReturnsNotFound_WhenStudentDoesNotExist()
    {
        // Arrange
        var updateDto = new StudentRequestDto
        {
            Name = "John Doe",
            Email = "john@example.com",
            IsAdmin = false
        };

        // Act
        var result = await _controller.PutStudent(999, updateDto);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task PutStudent_ReturnsBadRequest_WhenIdIsZero()
    {
        // Arrange
        var updateDto = new StudentRequestDto
        {
            Name = "John Doe",
            Email = "john@example.com",
            IsAdmin = false
        };

        // Act
        var result = await _controller.PutStudent(0, updateDto);

        // Assert
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task PutStudent_ReturnsBadRequest_WhenIdIsNegative()
    {
        // Arrange
        var updateDto = new StudentRequestDto
        {
            Name = "John Doe",
            Email = "john@example.com",
            IsAdmin = false
        };

        // Act
        var result = await _controller.PutStudent(-1, updateDto);

        // Assert
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task DeleteStudent_ReturnsNoContent_WhenStudentExists()
    {
        // Arrange
        var student = new Student { Name = "John Doe", Email = "john@example.com", IsAdmin = false };
        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteStudent(student.Id);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify student was deleted
        var deletedStudent = await _context.Students.FindAsync(student.Id);
        Assert.Null(deletedStudent);
    }

    [Fact]
    public async Task DeleteStudent_ReturnsNotFound_WhenStudentDoesNotExist()
    {
        // Act
        var result = await _controller.DeleteStudent(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
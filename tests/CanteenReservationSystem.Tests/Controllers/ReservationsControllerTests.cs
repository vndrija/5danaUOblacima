using API.Controllers;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Enums;
using CanteenReservationSystem.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanteenReservationSystem.Tests.Controllers;

public class ReservationsControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ReservationsController _controller;
    private readonly Student _student;
    private readonly Canteen _canteen;

    public ReservationsControllerTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext($"ReservationsTestDb_{Guid.NewGuid()}");
        var mapper = MapperHelper.CreateMapper();
        _controller = new ReservationsController(_context, mapper);

        // Create test data
        _student = new Student { Name = "John Doe", Email = "john@example.com", IsAdmin = false };
        _canteen = new Canteen
        {
            Name = "Main Canteen",
            Location = "Building A",
            Capacity = 10,
            WorkingHours = new List<WorkingHour>
            {
                new() { Meal = MealType.Breakfast, From = "08:00", To = "10:00" },
                new() { Meal = MealType.Lunch, From = "12:00", To = "14:00" }
            }
        };

        _context.Students.Add(_student);
        _context.Canteens.Add(_canteen);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task PostReservation_ReturnsCreatedReservation_WhenValidDataProvided()
    {
        // Arrange
        var tomorrow = DateTime.Today.AddDays(1);
        var reservationDto = new ReservationRequestDto
        {
            StudentId = _student.Id.ToString(),
            CanteenId = _canteen.Id.ToString(),
            Date = tomorrow.ToString("yyyy-MM-dd"),
            Time = "12:00",
            Duration = 30
        };

        // Act
        var result = await _controller.PostReservation(reservationDto);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ReservationResponseDto>>(result);
        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        var createdReservation = Assert.IsType<ReservationResponseDto>(createdResult.Value);
        Assert.Equal("12:00", createdReservation.Time);

        // Verify saved to database
        var savedReservation = await _context.Reservations.FirstOrDefaultAsync();
        Assert.NotNull(savedReservation);
        Assert.Equal(ReservationStatus.Active, savedReservation.Status);
    }

    [Fact]
    public async Task PostReservation_ReturnsBadRequest_WhenDateIsInPast()
    {
        // Arrange
        var yesterday = DateTime.Today.AddDays(-1);
        var reservationDto = new ReservationRequestDto
        {
            StudentId = _student.Id.ToString(),
            CanteenId = _canteen.Id.ToString(),
            Date = yesterday.ToString("yyyy-MM-dd"),
            Time = "12:00",
            Duration = 30
        };

        // Act
        var result = await _controller.PostReservation(reservationDto);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ReservationResponseDto>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task PostReservation_ReturnsBadRequest_WhenTimeIsNotOnHourOrHalfHour()
    {
        // Arrange
        var tomorrow = DateTime.Today.AddDays(1);
        var reservationDto = new ReservationRequestDto
        {
            StudentId = _student.Id.ToString(),
            CanteenId = _canteen.Id.ToString(),
            Date = tomorrow.ToString("yyyy-MM-dd"),
            Time = "12:15",
            Duration = 30
        };

        // Act
        var result = await _controller.PostReservation(reservationDto);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ReservationResponseDto>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task PostReservation_ReturnsBadRequest_WhenDurationIsNot30Or60()
    {
        // Arrange
        var tomorrow = DateTime.Today.AddDays(1);
        var reservationDto = new ReservationRequestDto
        {
            StudentId = _student.Id.ToString(),
            CanteenId = _canteen.Id.ToString(),
            Date = tomorrow.ToString("yyyy-MM-dd"),
            Time = "12:00",
            Duration = 45
        };

        // Act
        var result = await _controller.PostReservation(reservationDto);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ReservationResponseDto>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task DeleteReservation_ReturnsOkWithCancelledReservation_WhenStudentOwnsReservation()
    {
        // Arrange
        var reservation = new Reservation
        {
            StudentId = _student.Id,
            CanteenId = _canteen.Id,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Time = "12:00",
            Duration = 30,
            Status = ReservationStatus.Active
        };
        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteReservation(reservation.Id, _student.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var cancelledReservation = Assert.IsType<ReservationResponseDto>(okResult.Value);

        // Verify status is cancelled
        var updatedReservation = await _context.Reservations.FindAsync(reservation.Id);
        Assert.Equal(ReservationStatus.Cancelled, updatedReservation!.Status);
    }

    [Fact]
    public async Task GetReservation_ReturnsNotFound_WhenReservationDoesNotExist()
    {
        // Act
        var result = await _controller.GetReservation(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }
}

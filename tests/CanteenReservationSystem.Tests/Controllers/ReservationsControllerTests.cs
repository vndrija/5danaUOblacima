using API.Controllers;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Enums;
using API.Exceptions;
using API.Repositories;
using API.Services;
using CanteenReservationSystem.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanteenReservationSystem.Tests.Controllers;

public class ReservationsControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ReservationsController _controller;
    private readonly IReservationService _reservationService;
    private readonly Student _student;
    private readonly Canteen _canteen;

    public ReservationsControllerTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext($"ReservationsTestDb_{Guid.NewGuid()}");
        var mapper = MapperHelper.CreateMapper();
        var reservationRepository = new ReservationRepository(_context);
        var studentRepository = new StudentRepository(_context);
        var canteenRepository = new CanteenRepository(_context);
        _reservationService = new ReservationService(reservationRepository, studentRepository, canteenRepository, mapper);
        _controller = new ReservationsController(_reservationService);

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

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(async () =>
        {
            await _controller.PostReservation(reservationDto);
        });

        Assert.Equal("Reservation date cannot be in the past", exception.Message);
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

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(async () =>
        {
            await _controller.PostReservation(reservationDto);
        });

        Assert.Equal("Time must start on the hour or half-hour", exception.Message);
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

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(async () =>
        {
            await _controller.PostReservation(reservationDto);
        });

        Assert.Equal("Duration must be 30 or 60 minutes", exception.Message);
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

    [Fact]
    public async Task GetReservation_ReturnsReservation_WhenReservationExists()
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
        var result = await _controller.GetReservation(reservation.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReservation = Assert.IsType<ReservationResponseDto>(okResult.Value);
        Assert.Equal("12:00", returnedReservation.Time);
    }

    [Fact]
    public async Task GetReservations_ReturnsAllReservations()
    {
        // Arrange
        var reservation1 = new Reservation
        {
            StudentId = _student.Id,
            CanteenId = _canteen.Id,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Time = "12:00",
            Duration = 30,
            Status = ReservationStatus.Active
        };
        var reservation2 = new Reservation
        {
            StudentId = _student.Id,
            CanteenId = _canteen.Id,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            Time = "13:00",
            Duration = 60,
            Status = ReservationStatus.Active
        };
        _context.Reservations.AddRange(reservation1, reservation2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetReservations();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var reservations = Assert.IsAssignableFrom<IEnumerable<ReservationResponseDto>>(okResult.Value).ToList();
        Assert.Equal(2, reservations.Count);
    }

    [Fact]
    public async Task DeleteReservation_RemainsVisible_WithCancelledStatus()
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
        await _controller.DeleteReservation(reservation.Id, _student.Id);

        // Assert - Verify reservation still exists but with Cancelled status
        var cancelledReservation = await _context.Reservations.FindAsync(reservation.Id);
        Assert.NotNull(cancelledReservation);
        Assert.Equal(ReservationStatus.Cancelled, cancelledReservation.Status);

        // Verify it's still retrievable via GetReservation
        var getResult = await _controller.GetReservation(reservation.Id);
        var okResult = Assert.IsType<OkObjectResult>(getResult.Result);
        var retrievedReservation = Assert.IsType<ReservationResponseDto>(okResult.Value);
        Assert.Equal("Cancelled", retrievedReservation.Status);
    }

    [Fact]
    public async Task PostReservation_ReturnsBadRequest_WhenStudentHasOverlappingReservation()
    {
        // Arrange - Create first reservation
        var tomorrow = DateTime.Today.AddDays(1);
        var existingReservation = new Reservation
        {
            StudentId = _student.Id,
            CanteenId = _canteen.Id,
            Date = DateOnly.FromDateTime(tomorrow),
            Time = "12:00",
            Duration = 60, // 12:00 - 13:00
            Status = ReservationStatus.Active
        };
        _context.Reservations.Add(existingReservation);
        await _context.SaveChangesAsync();

        // Create second canteen
        var secondCanteen = new Canteen
        {
            Name = "Second Canteen",
            Location = "Building B",
            Capacity = 10,
            WorkingHours = new List<WorkingHour>
            {
                new() { Meal = MealType.Lunch, From = "12:00", To = "14:00" }
            }
        };
        _context.Canteens.Add(secondCanteen);
        await _context.SaveChangesAsync();

        // Try to create overlapping reservation in different canteen
        var overlappingDto = new ReservationRequestDto
        {
            StudentId = _student.Id.ToString(),
            CanteenId = secondCanteen.Id.ToString(),
            Date = tomorrow.ToString("yyyy-MM-dd"),
            Time = "12:30", // Overlaps with 12:00-13:00
            Duration = 30
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(async () =>
        {
            await _controller.PostReservation(overlappingDto);
        });

        Assert.Contains("already has a reservation", exception.Message.ToLower());
    }

    [Fact]
    public async Task PostReservation_ReturnsBadRequest_WhenCanteenIsFullyBooked()
    {
        // Arrange - Create a canteen with capacity of 1
        var smallCanteen = new Canteen
        {
            Name = "Small Canteen",
            Location = "Building C",
            Capacity = 1,
            WorkingHours = new List<WorkingHour>
            {
                new() { Meal = MealType.Lunch, From = "12:00", To = "14:00" }
            }
        };
        _context.Canteens.Add(smallCanteen);
        await _context.SaveChangesAsync();

        // Create a second student
        var student2 = new Student { Name = "Jane Doe", Email = "jane@example.com", IsAdmin = false };
        _context.Students.Add(student2);
        await _context.SaveChangesAsync();

        var tomorrow = DateTime.Today.AddDays(1);

        // First reservation fills the capacity
        var firstReservation = new Reservation
        {
            StudentId = _student.Id,
            CanteenId = smallCanteen.Id,
            Date = DateOnly.FromDateTime(tomorrow),
            Time = "12:00",
            Duration = 30,
            Status = ReservationStatus.Active
        };
        _context.Reservations.Add(firstReservation);
        await _context.SaveChangesAsync();

        // Try to create second reservation when capacity is full
        var secondDto = new ReservationRequestDto
        {
            StudentId = student2.Id.ToString(),
            CanteenId = smallCanteen.Id.ToString(),
            Date = tomorrow.ToString("yyyy-MM-dd"),
            Time = "12:00",
            Duration = 30
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(async () =>
        {
            await _controller.PostReservation(secondDto);
        });

        Assert.Contains("full capacity", exception.Message.ToLower());
    }

    [Fact]
    public async Task PostReservation_ReturnsBadRequest_WhenTimeIsOutsideWorkingHours()
    {
        // Arrange - Canteen only serves lunch from 12:00 to 14:00
        var tomorrow = DateTime.Today.AddDays(1);
        var reservationDto = new ReservationRequestDto
        {
            StudentId = _student.Id.ToString(),
            CanteenId = _canteen.Id.ToString(),
            Date = tomorrow.ToString("yyyy-MM-dd"),
            Time = "10:00", // Outside lunch hours (12:00-14:00)
            Duration = 30
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(async () =>
        {
            await _controller.PostReservation(reservationDto);
        });

        Assert.Contains("outside the canteen's", exception.Message.ToLower());
    }

    [Fact]
    public async Task PostReservation_AllowsMultipleReservations_InDifferentTimeSlots()
    {
        // Arrange - Create first reservation
        var tomorrow = DateTime.Today.AddDays(1);
        var firstReservation = new Reservation
        {
            StudentId = _student.Id,
            CanteenId = _canteen.Id,
            Date = DateOnly.FromDateTime(tomorrow),
            Time = "12:00",
            Duration = 30, // 12:00 - 12:30
            Status = ReservationStatus.Active
        };
        _context.Reservations.Add(firstReservation);
        await _context.SaveChangesAsync();

        // Try to create non-overlapping reservation
        var secondDto = new ReservationRequestDto
        {
            StudentId = _student.Id.ToString(),
            CanteenId = _canteen.Id.ToString(),
            Date = tomorrow.ToString("yyyy-MM-dd"),
            Time = "12:30", // Non-overlapping: 12:30 - 13:00
            Duration = 30
        };

        // Act
        var result = await _controller.PostReservation(secondDto);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ReservationResponseDto>>(result);
        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        Assert.NotNull(createdResult.Value);
    }
}

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
    public async Task GetReservations_ReturnsEmptyList_WhenNoReservationsExist()
    {
        // Act
        var result = await _controller.GetReservations();

        // Assert
        var actionResult = Assert.IsType<ActionResult<IEnumerable<ReservationResponseDto>>>(result);
        var reservations = Assert.IsAssignableFrom<IEnumerable<ReservationResponseDto>>(actionResult.Value);
        Assert.Empty(reservations);
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
        var actionResult = Assert.IsType<ActionResult<IEnumerable<ReservationResponseDto>>>(result);
        var reservations = Assert.IsAssignableFrom<IEnumerable<ReservationResponseDto>>(actionResult.Value).ToList();
        Assert.Equal(2, reservations.Count);
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
        var actionResult = Assert.IsType<ActionResult<ReservationResponseDto>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returnedReservation = Assert.IsType<ReservationResponseDto>(okResult.Value);
        Assert.Equal(reservation.Id.ToString(), returnedReservation.Id);
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
    public async Task PostReservation_ReturnsBadRequest_WhenStudentDoesNotExist()
    {
        // Arrange
        var tomorrow = DateTime.Today.AddDays(1);
        var reservationDto = new ReservationRequestDto
        {
            StudentId = "999",
            CanteenId = _canteen.Id.ToString(),
            Date = tomorrow.ToString("yyyy-MM-dd"),
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
    public async Task PostReservation_ReturnsBadRequest_WhenCanteenDoesNotExist()
    {
        // Arrange
        var tomorrow = DateTime.Today.AddDays(1);
        var reservationDto = new ReservationRequestDto
        {
            StudentId = _student.Id.ToString(),
            CanteenId = "999",
            Date = tomorrow.ToString("yyyy-MM-dd"),
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
    public async Task PostReservation_ReturnsBadRequest_WhenTimeIsOutsideWorkingHours()
    {
        // Arrange
        var tomorrow = DateTime.Today.AddDays(1);
        var reservationDto = new ReservationRequestDto
        {
            StudentId = _student.Id.ToString(),
            CanteenId = _canteen.Id.ToString(),
            Date = tomorrow.ToString("yyyy-MM-dd"),
            Time = "15:00", // Outside working hours
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
    public async Task PostReservation_ReturnsBadRequest_WhenReservationEndsAfterWorkingHours()
    {
        // Arrange
        var tomorrow = DateTime.Today.AddDays(1);
        var reservationDto = new ReservationRequestDto
        {
            StudentId = _student.Id.ToString(),
            CanteenId = _canteen.Id.ToString(),
            Date = tomorrow.ToString("yyyy-MM-dd"),
            Time = "13:30", // Ends at 14:30, but working hours end at 14:00
            Duration = 60
        };

        // Act
        var result = await _controller.PostReservation(reservationDto);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ReservationResponseDto>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task PostReservation_ReturnsBadRequest_WhenStudentHasOverlappingReservation()
    {
        // Arrange
        var tomorrow = DateTime.Today.AddDays(1);
        var existingReservation = new Reservation
        {
            StudentId = _student.Id,
            CanteenId = _canteen.Id,
            Date = DateOnly.FromDateTime(tomorrow),
            Time = "12:00",
            Duration = 60,
            Status = ReservationStatus.Active
        };
        _context.Reservations.Add(existingReservation);
        await _context.SaveChangesAsync();

        var reservationDto = new ReservationRequestDto
        {
            StudentId = _student.Id.ToString(),
            CanteenId = _canteen.Id.ToString(),
            Date = tomorrow.ToString("yyyy-MM-dd"),
            Time = "12:30", // Overlaps with existing 12:00-13:00
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
    public async Task PostReservation_ReturnsBadRequest_WhenCanteenIsAtFullCapacity()
    {
        // Arrange
        var tomorrow = DateTime.Today.AddDays(1);
        var date = DateOnly.FromDateTime(tomorrow);

        // Create 10 reservations (canteen capacity is 10)
        for (int i = 0; i < 10; i++)
        {
            var otherStudent = new Student
            {
                Name = $"Student {i}",
                Email = $"student{i}@example.com",
                IsAdmin = false
            };
            _context.Students.Add(otherStudent);
            await _context.SaveChangesAsync();

            var reservation = new Reservation
            {
                StudentId = otherStudent.Id,
                CanteenId = _canteen.Id,
                Date = date,
                Time = "12:00",
                Duration = 30,
                Status = ReservationStatus.Active
            };
            _context.Reservations.Add(reservation);
        }
        await _context.SaveChangesAsync();

        var newStudent = new Student
        {
            Name = "New Student",
            Email = "new@example.com",
            IsAdmin = false
        };
        _context.Students.Add(newStudent);
        await _context.SaveChangesAsync();

        var reservationDto = new ReservationRequestDto
        {
            StudentId = newStudent.Id.ToString(),
            CanteenId = _canteen.Id.ToString(),
            Date = tomorrow.ToString("yyyy-MM-dd"),
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
    public async Task PostReservation_ReturnsCreated_WhenCancelledReservationsDontCountTowardsCapacity()
    {
        // Arrange
        var tomorrow = DateTime.Today.AddDays(1);
        var date = DateOnly.FromDateTime(tomorrow);

        // Create a cancelled reservation
        var cancelledReservation = new Reservation
        {
            StudentId = _student.Id,
            CanteenId = _canteen.Id,
            Date = date,
            Time = "12:00",
            Duration = 30,
            Status = ReservationStatus.Cancelled
        };
        _context.Reservations.Add(cancelledReservation);
        await _context.SaveChangesAsync();

        var reservationDto = new ReservationRequestDto
        {
            StudentId = _student.Id.ToString(),
            CanteenId = _canteen.Id.ToString(),
            Date = tomorrow.ToString("yyyy-MM-dd"),
            Time = "12:30", // Different time, so no overlap
            Duration = 30
        };

        // Act
        var result = await _controller.PostReservation(reservationDto);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ReservationResponseDto>>(result);
        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        Assert.NotNull(createdResult.Value);
    }

    [Fact]
    public async Task PostReservation_ReturnsBadRequest_WhenInvalidStudentIdFormat()
    {
        // Arrange
        var tomorrow = DateTime.Today.AddDays(1);
        var reservationDto = new ReservationRequestDto
        {
            StudentId = "invalid",
            CanteenId = _canteen.Id.ToString(),
            Date = tomorrow.ToString("yyyy-MM-dd"),
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
    public async Task PostReservation_ReturnsBadRequest_WhenInvalidDateFormat()
    {
        // Arrange
        var reservationDto = new ReservationRequestDto
        {
            StudentId = _student.Id.ToString(),
            CanteenId = _canteen.Id.ToString(),
            Date = "invalid-date",
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
    public async Task PutReservation_ReturnsNoContent_WhenUpdateSuccessful()
    {
        // Arrange
        var tomorrow = DateTime.Today.AddDays(1);
        var reservation = new Reservation
        {
            StudentId = _student.Id,
            CanteenId = _canteen.Id,
            Date = DateOnly.FromDateTime(tomorrow),
            Time = "12:00",
            Duration = 30,
            Status = ReservationStatus.Active
        };
        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        var updateDto = new ReservationRequestDto
        {
            StudentId = _student.Id.ToString(),
            CanteenId = _canteen.Id.ToString(),
            Date = tomorrow.ToString("yyyy-MM-dd"),
            Time = "13:00",
            Duration = 60
        };

        // Act
        var result = await _controller.PutReservation(reservation.Id, updateDto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify update
        var updatedReservation = await _context.Reservations.FindAsync(reservation.Id);
        Assert.Equal("13:00", updatedReservation!.Time);
        Assert.Equal(60, updatedReservation.Duration);
    }

    [Fact]
    public async Task PutReservation_ReturnsNotFound_WhenReservationDoesNotExist()
    {
        // Arrange
        var updateDto = new ReservationRequestDto
        {
            StudentId = _student.Id.ToString(),
            CanteenId = _canteen.Id.ToString(),
            Date = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"),
            Time = "12:00",
            Duration = 30
        };

        // Act
        var result = await _controller.PutReservation(999, updateDto);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task PutReservation_ReturnsBadRequest_WhenIdIsZeroOrNegative()
    {
        // Arrange
        var updateDto = new ReservationRequestDto
        {
            StudentId = _student.Id.ToString(),
            CanteenId = _canteen.Id.ToString(),
            Date = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"),
            Time = "12:00",
            Duration = 30
        };

        // Act
        var result = await _controller.PutReservation(0, updateDto);

        // Assert
        Assert.IsType<BadRequestResult>(result);
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
    public async Task DeleteReservation_ReturnsNotFound_WhenReservationDoesNotExist()
    {
        // Act
        var result = await _controller.DeleteReservation(999, _student.Id);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteReservation_ReturnsForbidden_WhenStudentDoesNotOwnReservation()
    {
        // Arrange
        var otherStudent = new Student { Name = "Other Student", Email = "other@example.com", IsAdmin = false };
        _context.Students.Add(otherStudent);
        await _context.SaveChangesAsync();

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
        var result = await _controller.DeleteReservation(reservation.Id, otherStudent.Id);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task DeleteReservation_ReturnsBadRequest_WhenReservationIsAlreadyCancelled()
    {
        // Arrange
        var reservation = new Reservation
        {
            StudentId = _student.Id,
            CanteenId = _canteen.Id,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Time = "12:00",
            Duration = 30,
            Status = ReservationStatus.Cancelled
        };
        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteReservation(reservation.Id, _student.Id);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }
}
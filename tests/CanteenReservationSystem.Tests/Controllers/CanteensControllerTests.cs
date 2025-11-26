using API.Controllers;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Enums;
using CanteenReservationSystem.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanteenReservationSystem.Tests.Controllers;

public class CanteensControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly CanteensController _controller;
    private readonly Student _adminStudent;
    private readonly Student _regularStudent;

    public CanteensControllerTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext($"CanteensTestDb_{Guid.NewGuid()}");
        var mapper = MapperHelper.CreateMapper();
        _controller = new CanteensController(_context, mapper);

        // Create test students
        _adminStudent = new Student { Name = "Admin User", Email = "admin@example.com", IsAdmin = true };
        _regularStudent = new Student { Name = "Regular User", Email = "user@example.com", IsAdmin = false };
        _context.Students.AddRange(_adminStudent, _regularStudent);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetCanteens_ReturnsEmptyList_WhenNoCanteensExist()
    {
        // Act
        var result = await _controller.GetCanteens();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var canteens = Assert.IsAssignableFrom<IEnumerable<CanteenResponseDto>>(okResult.Value);
        Assert.Empty(canteens);
    }

    [Fact]
    public async Task GetCanteens_ReturnsAllCanteensWithWorkingHours()
    {
        // Arrange
        var canteen1 = new Canteen
        {
            Name = "Main Canteen",
            Location = "Building A",
            Capacity = 100,
            WorkingHours = new List<WorkingHour>
            {
                new() { Meal = MealType.Breakfast, From = "08:00", To = "10:00" }
            }
        };
        var canteen2 = new Canteen
        {
            Name = "Secondary Canteen",
            Location = "Building B",
            Capacity = 50,
            WorkingHours = new List<WorkingHour>
            {
                new() { Meal = MealType.Lunch, From = "12:00", To = "14:00" }
            }
        };
        _context.Canteens.AddRange(canteen1, canteen2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetCanteens();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var canteens = Assert.IsAssignableFrom<IEnumerable<CanteenResponseDto>>(okResult.Value).ToList();
        Assert.Equal(2, canteens.Count);
        Assert.All(canteens, c => Assert.NotNull(c.WorkingHours));
    }

    [Fact]
    public async Task GetCanteen_ReturnsNotFound_WhenCanteenDoesNotExist()
    {
        // Act
        var result = await _controller.GetCanteen(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetCanteen_ReturnsCanteen_WhenCanteenExists()
    {
        // Arrange
        var canteen = new Canteen
        {
            Name = "Main Canteen",
            Location = "Building A",
            Capacity = 100,
            WorkingHours = new List<WorkingHour>
            {
                new() { Meal = MealType.Breakfast, From = "08:00", To = "10:00" }
            }
        };
        _context.Canteens.Add(canteen);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetCanteen(canteen.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCanteen = Assert.IsType<CanteenResponseDto>(okResult.Value);
        Assert.Equal("Main Canteen", returnedCanteen.Name);
        Assert.Single(returnedCanteen.WorkingHours);
    }

    [Fact]
    public async Task PostCanteen_ReturnsCreatedCanteen_WhenAdminCreatesValidCanteen()
    {
        // Arrange
        var canteenDto = new CreateCanteenRequestDto
        {
            Name = "New Canteen",
            Location = "Building C",
            Capacity = 75,
            WorkingHours = new List<WorkingHourDto>
            {
                new() { Meal = "breakfast", From = "07:00", To = "09:00" }
            }
        };

        // Act
        var result = await _controller.PostCanteen(canteenDto, _adminStudent.Id);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var createdCanteen = Assert.IsType<CanteenResponseDto>(createdResult.Value);
        Assert.Equal("New Canteen", createdCanteen.Name);

        // Verify saved to database
        var savedCanteen = await _context.Canteens
            .Include(c => c.WorkingHours)
            .FirstOrDefaultAsync(c => c.Name == "New Canteen");
        Assert.NotNull(savedCanteen);
        Assert.Single(savedCanteen.WorkingHours);
    }

    [Fact]
    public async Task PostCanteen_ReturnsForbidden_WhenNonAdminAttemptsToCreate()
    {
        // Arrange
        var canteenDto = new CreateCanteenRequestDto
        {
            Name = "New Canteen",
            Location = "Building C",
            Capacity = 75,
            WorkingHours = new List<WorkingHourDto>
            {
                new() { Meal = "breakfast", From = "07:00", To = "09:00" }
            }
        };

        // Act
        var result = await _controller.PostCanteen(canteenDto, _regularStudent.Id);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task PostCanteen_ReturnsForbidden_WhenStudentDoesNotExist()
    {
        // Arrange
        var canteenDto = new CreateCanteenRequestDto
        {
            Name = "New Canteen",
            Location = "Building C",
            Capacity = 75,
            WorkingHours = new List<WorkingHourDto>
            {
                new() { Meal = "breakfast", From = "07:00", To = "09:00" }
            }
        };

        // Act
        var result = await _controller.PostCanteen(canteenDto, 999);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task PostCanteen_ReturnsBadRequest_WhenWorkingHoursAreNull()
    {
        // Arrange
        var canteenDto = new CreateCanteenRequestDto
        {
            Name = "New Canteen",
            Location = "Building C",
            Capacity = 75,
            WorkingHours = null!
        };

        // Act
        var result = await _controller.PostCanteen(canteenDto, _adminStudent.Id);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task PostCanteen_ReturnsBadRequest_WhenWorkingHoursAreEmpty()
    {
        // Arrange
        var canteenDto = new CreateCanteenRequestDto
        {
            Name = "New Canteen",
            Location = "Building C",
            Capacity = 75,
            WorkingHours = new List<WorkingHourDto>()
        };

        // Act
        var result = await _controller.PostCanteen(canteenDto, _adminStudent.Id);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task PutCanteen_ReturnsOk_WhenAdminUpdatesCanteen()
    {
        // Arrange
        var canteen = new Canteen
        {
            Name = "Old Name",
            Location = "Building A",
            Capacity = 100,
            WorkingHours = new List<WorkingHour>
            {
                new() { Meal = MealType.Breakfast, From = "08:00", To = "10:00" }
            }
        };
        _context.Canteens.Add(canteen);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateCanteenRequestDto
        {
            Name = "Updated Name",
            Location = "Building B",
            Capacity = 150
        };

        // Act
        var result = await _controller.PutCanteen(canteen.Id, updateDto, _adminStudent.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updatedCanteen = Assert.IsType<CanteenResponseDto>(okResult.Value);
        Assert.Equal("Updated Name", updatedCanteen.Name);
        Assert.Equal("Building B", updatedCanteen.Location);
        Assert.Equal(150, updatedCanteen.Capacity);
    }

    [Fact]
    public async Task PutCanteen_ReturnsForbidden_WhenNonAdminAttemptsToUpdate()
    {
        // Arrange
        var canteen = new Canteen
        {
            Name = "Main Canteen",
            Location = "Building A",
            Capacity = 100,
            WorkingHours = new List<WorkingHour>
            {
                new() { Meal = MealType.Breakfast, From = "08:00", To = "10:00" }
            }
        };
        _context.Canteens.Add(canteen);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateCanteenRequestDto { Name = "Updated Name" };

        // Act
        var result = await _controller.PutCanteen(canteen.Id, updateDto, _regularStudent.Id);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task PutCanteen_ReturnsNotFound_WhenCanteenDoesNotExist()
    {
        // Arrange
        var updateDto = new UpdateCanteenRequestDto { Name = "Updated Name" };

        // Act
        var result = await _controller.PutCanteen(999, updateDto, _adminStudent.Id);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task PutCanteen_ReturnsBadRequest_WhenUpdateDtoIsNull()
    {
        // Act
        var result = await _controller.PutCanteen(1, null!, _adminStudent.Id);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteCanteen_ReturnsNoContent_WhenAdminDeletesCanteen()
    {
        // Arrange
        var canteen = new Canteen
        {
            Name = "Main Canteen",
            Location = "Building A",
            Capacity = 100,
            WorkingHours = new List<WorkingHour>
            {
                new() { Meal = MealType.Breakfast, From = "08:00", To = "10:00" }
            }
        };
        _context.Canteens.Add(canteen);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteCanteen(canteen.Id, _adminStudent.Id);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify deletion
        var deletedCanteen = await _context.Canteens.FindAsync(canteen.Id);
        Assert.Null(deletedCanteen);
    }

    [Fact]
    public async Task DeleteCanteen_CancelsActiveReservations_WhenCanteenIsDeleted()
    {
        // Arrange
        var canteen = new Canteen
        {
            Name = "Main Canteen",
            Location = "Building A",
            Capacity = 100,
            WorkingHours = new List<WorkingHour>
            {
                new() { Meal = MealType.Lunch, From = "12:00", To = "14:00" }
            }
        };
        _context.Canteens.Add(canteen);
        await _context.SaveChangesAsync();

        var reservation = new Reservation
        {
            StudentId = _regularStudent.Id,
            CanteenId = canteen.Id,
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
            Time = "12:00",
            Duration = 30,
            Status = ReservationStatus.Active
        };
        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteCanteen(canteen.Id, _adminStudent.Id);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify reservation is cancelled
        var cancelledReservation = await _context.Reservations.FindAsync(reservation.Id);
        Assert.Equal(ReservationStatus.Cancelled, cancelledReservation!.Status);
    }

    [Fact]
    public async Task DeleteCanteen_ReturnsForbidden_WhenNonAdminAttemptsToDelete()
    {
        // Arrange
        var canteen = new Canteen
        {
            Name = "Main Canteen",
            Location = "Building A",
            Capacity = 100,
            WorkingHours = new List<WorkingHour>
            {
                new() { Meal = MealType.Breakfast, From = "08:00", To = "10:00" }
            }
        };
        _context.Canteens.Add(canteen);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteCanteen(canteen.Id, _regularStudent.Id);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task DeleteCanteen_ReturnsNotFound_WhenCanteenDoesNotExist()
    {
        // Act
        var result = await _controller.DeleteCanteen(999, _adminStudent.Id);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetCanteensStatus_ReturnsBadRequest_WhenInvalidDateFormat()
    {
        // Act
        var result = await _controller.GetCanteensStatus("invalid", "2025-12-01", "12:00", "14:00", 30);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetCanteensStatus_ReturnsBadRequest_WhenStartDateAfterEndDate()
    {
        // Act
        var result = await _controller.GetCanteensStatus("2025-12-31", "2025-12-01", "12:00", "14:00", 30);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetCanteensStatus_ReturnsBadRequest_WhenDurationIsInvalid()
    {
        // Act
        var result = await _controller.GetCanteensStatus("2025-12-01", "2025-12-01", "12:00", "14:00", 45);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetCanteensStatus_ReturnsAvailableSlots_WhenValidParametersProvided()
    {
        // Arrange
        var canteen = new Canteen
        {
            Name = "Main Canteen",
            Location = "Building A",
            Capacity = 100,
            WorkingHours = new List<WorkingHour>
            {
                new() { Meal = MealType.Lunch, From = "12:00", To = "14:00" }
            }
        };
        _context.Canteens.Add(canteen);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetCanteensStatus("2025-12-01", "2025-12-01", "12:00", "14:00", 30);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var statusResponse = Assert.IsAssignableFrom<IEnumerable<CanteenStatusResponseDto>>(okResult.Value).ToList();
        Assert.Single(statusResponse);
        Assert.NotEmpty(statusResponse.First().Slots);
    }

    [Fact]
    public async Task GetCanteenStatus_ReturnsNotFound_WhenCanteenDoesNotExist()
    {
        // Act
        var result = await _controller.GetCanteenStatus(999, "2025-12-01", "2025-12-01", "12:00", "14:00", 30);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetCanteenStatus_CalculatesRemainingCapacity_WithExistingReservations()
    {
        // Arrange
        var canteen = new Canteen
        {
            Name = "Main Canteen",
            Location = "Building A",
            Capacity = 10,
            WorkingHours = new List<WorkingHour>
            {
                new() { Meal = MealType.Lunch, From = "12:00", To = "14:00" }
            }
        };
        _context.Canteens.Add(canteen);
        await _context.SaveChangesAsync();

        var date = DateOnly.Parse("2025-12-15");
        var reservation = new Reservation
        {
            StudentId = _regularStudent.Id,
            CanteenId = canteen.Id,
            Date = date,
            Time = "12:00",
            Duration = 30,
            Status = ReservationStatus.Active
        };
        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetCanteenStatus(canteen.Id, "2025-12-15", "2025-12-15", "12:00", "14:00", 30);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var statusResponse = Assert.IsType<CanteenStatusResponseDto>(okResult.Value);

        var slotAt12 = statusResponse.Slots.FirstOrDefault(s => s.StartTime == "12:00");
        Assert.NotNull(slotAt12);
        Assert.Equal(9, slotAt12.RemainingCapacity); // 10 - 1 reservation
    }
}
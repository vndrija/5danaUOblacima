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

public class CanteensControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly CanteensController _controller;
    private readonly ICanteenService _canteenService;
    private readonly Student _adminStudent;
    private readonly Student _regularStudent;

    public CanteensControllerTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext($"CanteensTestDb_{Guid.NewGuid()}");
        var mapper = MapperHelper.CreateMapper();
        var canteenRepository = new CanteenRepository(_context);
        var studentRepository = new StudentRepository(_context);
        _canteenService = new CanteenService(canteenRepository, studentRepository, mapper);
        _controller = new CanteensController(_canteenService);

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

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ForbiddenException>(async () =>
        {
            await _controller.PostCanteen(canteenDto, _regularStudent.Id);
        });

        Assert.Equal("Only an admin can create a canteen.", exception.Message);
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

        // Verify canteen is deleted
        var deletedCanteen = await _context.Canteens.FindAsync(canteen.Id);
        Assert.Null(deletedCanteen);

        // Verify reservation was deleted along with the canteen
        var deletedReservation = await _context.Reservations.FindAsync(reservation.Id);
        Assert.Null(deletedReservation);
    }

    [Fact]
    public async Task GetCanteen_ReturnsCanteenWithWorkingHours_WhenCanteenExists()
    {
        // Arrange
        var canteen = new Canteen
        {
            Name = "Test Canteen",
            Location = "Building A",
            Capacity = 100,
            WorkingHours = new List<WorkingHour>
            {
                new() { Meal = MealType.Breakfast, From = "08:00", To = "10:00" },
                new() { Meal = MealType.Lunch, From = "12:00", To = "14:00" }
            }
        };
        _context.Canteens.Add(canteen);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetCanteen(canteen.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCanteen = Assert.IsType<CanteenResponseDto>(okResult.Value);
        Assert.Equal("Test Canteen", returnedCanteen.Name);
        Assert.Equal(2, returnedCanteen.WorkingHours.Count);
    }

    [Fact]
    public async Task PutCanteen_UpdatesCanteen_WhenAdminUpdates()
    {
        // Arrange
        var canteen = new Canteen
        {
            Name = "Original Canteen",
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
            Name = "Updated Canteen",
            Location = "Building B",
            Capacity = 150,
            WorkingHours = new List<WorkingHourDto>
            {
                new() { Meal = "lunch", From = "12:00", To = "14:00" }
            }
        };

        // Act
        var result = await _controller.PutCanteen(canteen.Id, updateDto, _adminStudent.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updatedCanteen = Assert.IsType<CanteenResponseDto>(okResult.Value);
        Assert.Equal("Updated Canteen", updatedCanteen.Name);
        Assert.Equal("Building B", updatedCanteen.Location);
        Assert.Equal(150, updatedCanteen.Capacity);
    }

    [Fact]
    public async Task PutCanteen_ReturnsForbidden_WhenNonAdminAttemptsToUpdate()
    {
        // Arrange
        var canteen = new Canteen
        {
            Name = "Test Canteen",
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
            Name = "Updated Canteen",
            Location = "Building B",
            Capacity = 150,
            WorkingHours = new List<WorkingHourDto>
            {
                new() { Meal = "lunch", From = "12:00", To = "14:00" }
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ForbiddenException>(async () =>
        {
            await _controller.PutCanteen(canteen.Id, updateDto, _regularStudent.Id);
        });

        Assert.Equal("Only an admin can update the canteen.", exception.Message);
    }

    [Fact]
    public async Task DeleteCanteen_ReturnsForbidden_WhenNonAdminAttemptsToDelete()
    {
        // Arrange
        var canteen = new Canteen
        {
            Name = "Test Canteen",
            Location = "Building A",
            Capacity = 100,
            WorkingHours = new List<WorkingHour>
            {
                new() { Meal = MealType.Lunch, From = "12:00", To = "14:00" }
            }
        };
        _context.Canteens.Add(canteen);
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ForbiddenException>(async () =>
        {
            await _controller.DeleteCanteen(canteen.Id, _regularStudent.Id);
        });

        Assert.Equal("Only an admin can delete the canteen.", exception.Message);
    }

    [Fact]
    public async Task GetCanteensStatus_ReturnsAvailableSlots_ForDateRangeAndTimeInterval()
    {
        // Arrange
        var canteen = new Canteen
        {
            Name = "Test Canteen",
            Location = "Building A",
            Capacity = 10,
            WorkingHours = new List<WorkingHour>
            {
                new() { Meal = MealType.Lunch, From = "12:00", To = "14:00" }
            }
        };
        _context.Canteens.Add(canteen);
        await _context.SaveChangesAsync();

        var tomorrow = DateTime.Today.AddDays(1);

        // Act
        var result = await _controller.GetCanteensStatus(
            tomorrow.ToString("yyyy-MM-dd"),
            tomorrow.ToString("yyyy-MM-dd"),
            "12:00",
            "13:00",
            30);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var statuses = Assert.IsAssignableFrom<IEnumerable<CanteenStatusResponseDto>>(okResult.Value).ToList();
        Assert.Single(statuses);

        var status = statuses[0];
        Assert.NotEmpty(status.Slots);

        // Should have slots: 12:00 and 12:30 (within 12:00-13:00 for 30 min duration)
        Assert.True(status.Slots.Count >= 2);
    }

    [Fact]
    public async Task GetCanteenStatus_ReturnsAvailableSlots_ForSpecificCanteen()
    {
        // Arrange
        var canteen = new Canteen
        {
            Name = "Test Canteen",
            Location = "Building A",
            Capacity = 5,
            WorkingHours = new List<WorkingHour>
            {
                new() { Meal = MealType.Lunch, From = "12:00", To = "14:00" }
            }
        };
        _context.Canteens.Add(canteen);
        await _context.SaveChangesAsync();

        var tomorrow = DateTime.Today.AddDays(1);

        // Create some reservations to reduce available capacity
        var reservation = new Reservation
        {
            StudentId = _regularStudent.Id,
            CanteenId = canteen.Id,
            Date = DateOnly.FromDateTime(tomorrow),
            Time = "12:00",
            Duration = 30,
            Status = ReservationStatus.Active
        };
        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetCanteenStatus(
            canteen.Id,
            tomorrow.ToString("yyyy-MM-dd"),
            tomorrow.ToString("yyyy-MM-dd"),
            "12:00",
            "13:00",
            30);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<CanteenStatusResponseDto>(okResult.Value);
        Assert.NotEmpty(status.Slots);

        // Find the 12:00 slot and verify capacity is reduced
        var slot1200 = status.Slots.FirstOrDefault(s => s.StartTime == "12:00");
        Assert.NotNull(slot1200);
        Assert.Equal(4, slot1200.RemainingCapacity); // 5 - 1 reservation
    }

    [Fact]
    public async Task GetCanteensStatus_ReturnsMultipleDays_WhenDateRangeSpansMultipleDays()
    {
        // Arrange
        var canteen = new Canteen
        {
            Name = "Test Canteen",
            Location = "Building A",
            Capacity = 10,
            WorkingHours = new List<WorkingHour>
            {
                new() { Meal = MealType.Lunch, From = "12:00", To = "14:00" }
            }
        };
        _context.Canteens.Add(canteen);
        await _context.SaveChangesAsync();

        var startDate = DateTime.Today.AddDays(1);
        var endDate = DateTime.Today.AddDays(3);

        // Act
        var result = await _controller.GetCanteensStatus(
            startDate.ToString("yyyy-MM-dd"),
            endDate.ToString("yyyy-MM-dd"),
            "12:00",
            "13:00",
            30);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var statuses = Assert.IsAssignableFrom<IEnumerable<CanteenStatusResponseDto>>(okResult.Value).ToList();
        Assert.Single(statuses);

        var status = statuses[0];
        // Should have slots for 3 days (day 1, 2, 3) * 2 slots per day (12:00, 12:30) = 6 slots
        Assert.True(status.Slots.Count >= 6);
    }
}

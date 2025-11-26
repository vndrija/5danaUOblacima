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
}

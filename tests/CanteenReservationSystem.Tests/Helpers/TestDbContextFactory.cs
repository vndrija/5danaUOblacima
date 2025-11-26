using API.Data;
using Microsoft.EntityFrameworkCore;

namespace CanteenReservationSystem.Tests.Helpers;

public static class TestDbContextFactory
{
    public static AppDbContext CreateInMemoryContext(string databaseName = "TestDb")
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static AppDbContext CreateInMemoryContextWithAutoMapper()
    {
        return CreateInMemoryContext($"TestDb_{Guid.NewGuid()}");
    }
}
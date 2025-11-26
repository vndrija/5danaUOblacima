using API.Mappings;
using AutoMapper;

namespace CanteenReservationSystem.Tests.Helpers;

public static class MapperHelper
{
    public static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<StudentProfile>();
            cfg.AddProfile<CanteenProfile>();
            cfg.AddProfile<ReservationProfile>();
            cfg.AddProfile<WorkingHourProfile>();
        });

        return config.CreateMapper();
    }
}
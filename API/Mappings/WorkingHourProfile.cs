using System;
using API.DTOs;
using API.Entities;
using API.Enums;
using AutoMapper;

namespace API.Mappings;

public class WorkingHourProfile : Profile
{
    public WorkingHourProfile()
    {
        CreateMap<WorkingHour, WorkingHourDto>()
            .ForMember(dest => dest.Meal,
                opt => opt.MapFrom(src => src.Meal.ToString().ToLower()));

        // Needed for CreateCanteenRequestDto.WorkingHours
        CreateMap<WorkingHourDto, WorkingHour>()
            .ForMember(dest => dest.Meal,
                opt => opt.MapFrom(src => Enum.Parse<MealType>(src.Meal, true)));
    }

}

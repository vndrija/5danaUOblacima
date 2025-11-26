using System;
using API.DTOs;
using API.Entities;
using AutoMapper;

namespace API.Mappings;

public class CanteenProfile : Profile
{
    public CanteenProfile()
    {
        CreateMap<Canteen, CanteenResponseDto>();
        CreateMap<CreateCanteenRequestDto, Canteen>();
        CreateMap<UpdateCanteenRequestDto, Canteen>()
            .ForMember(dest => dest.WorkingHours, opt => opt.Ignore())
            .ForMember(dest => dest.Name, opt => opt.Condition(src => src.Name != null))
            .ForMember(dest => dest.Location, opt => opt.Condition(src => src.Location != null))
            .ForMember(dest => dest.Capacity, opt => opt.Condition(src => src.Capacity != null && src.Capacity > 0));
    }
}

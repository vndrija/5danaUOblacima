using System;
using API.DTOs;
using API.Entities;
using AutoMapper;

namespace API.Mappings;

public class ReservationProfile : Profile
{
    public ReservationProfile()
    {
        CreateMap<Reservation, ReservationResponseDto>()
        .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Date.ToString("yyyy-MM-dd")));
        CreateMap<ReservationResponseDto, Reservation>();
        CreateMap<ReservationRequestDto, Reservation>()
            .ForMember(dest => dest.Date, opt => opt.MapFrom(src => DateOnly.Parse(src.Date)));
    }

}

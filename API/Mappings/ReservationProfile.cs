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
        .ForMember(dest => dest.Id,
                opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.StudentId,
                opt => opt.MapFrom(src => src.StudentId.ToString()))
            .ForMember(dest => dest.CanteenId,
                opt => opt.MapFrom(src => src.CanteenId.ToString()))
            .ForMember(dest => dest.Date,
                opt => opt.MapFrom(src => src.Date.ToString("yyyy-MM-dd")));
        CreateMap<ReservationResponseDto, Reservation>()
        .ForMember(dest => dest.StudentId,
                opt => opt.MapFrom(src => int.Parse(src.StudentId)))
            .ForMember(dest => dest.CanteenId,
                opt => opt.MapFrom(src => int.Parse(src.CanteenId)))
            .ForMember(dest => dest.Date,
                opt => opt.MapFrom(src => DateOnly.Parse(src.Date))); ;
        CreateMap<ReservationRequestDto, Reservation>()
            .ForMember(dest => dest.Date, opt => opt.MapFrom(src => DateOnly.Parse(src.Date)));
    }

}

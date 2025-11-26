using System;
using API.DTOs;
using API.Entities;
using AutoMapper;

namespace API.Mappings;

public class StudentProfile : Profile
{
    public StudentProfile()
    {
        CreateMap<Student, StudentResponseDto>()
        .ForMember(dest => dest.Id,
                opt => opt.MapFrom(src => src.Id.ToString())); ;
        CreateMap<StudentResponseDto, Student>();
        CreateMap<StudentRequestDto, Student>();
    }
}

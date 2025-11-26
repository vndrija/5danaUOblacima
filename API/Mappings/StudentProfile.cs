using System;
using API.DTOs;
using API.Entities;
using AutoMapper;

namespace API.Mappings;

public class StudentProfile : Profile
{
    public StudentProfile()
    {
        CreateMap<Student, StudentResponseDto>();
        CreateMap<StudentResponseDto, Student>();
        CreateMap<StudentRequestDto, Student>();
    }
}

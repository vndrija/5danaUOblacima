using System;

namespace API.DTOs;

public class StudentRequestDto
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public bool IsAdmin { get; set; }
}

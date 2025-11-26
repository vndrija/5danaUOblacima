using API.DTOs;

namespace API.Services
{
    public interface ICanteenService
    {
        Task<IEnumerable<CanteenResponseDto>> GetAllCanteensAsync();
        Task<CanteenResponseDto?> GetCanteenByIdAsync(int id);
        Task<CanteenResponseDto> CreateCanteenAsync(CreateCanteenRequestDto canteenDto, int studentId);
        Task<CanteenResponseDto?> UpdateCanteenAsync(int id, UpdateCanteenRequestDto canteenDto, int studentId);
        Task<bool> DeleteCanteenAsync(int id, int studentId);
        Task<IEnumerable<CanteenStatusResponseDto>> GetCanteensStatusAsync(
            string startDate, string endDate, string startTime, string endTime, int duration);
        Task<CanteenStatusResponseDto?> GetCanteenStatusAsync(
            int id, string startDate, string endDate, string startTime, string endTime, int duration);
    }
}

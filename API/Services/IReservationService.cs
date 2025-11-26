using API.DTOs;

namespace API.Services
{
    public interface IReservationService
    {
        Task<IEnumerable<ReservationResponseDto>> GetAllReservationsAsync();
        Task<ReservationResponseDto?> GetReservationByIdAsync(int id);
        Task<ReservationResponseDto> CreateReservationAsync(ReservationRequestDto reservationDto);
        Task<bool> UpdateReservationAsync(int id, ReservationRequestDto reservationDto);
        Task<ReservationResponseDto?> CancelReservationAsync(int id, int studentId);
        Task<bool> ReservationExistsAsync(int id);
    }
}

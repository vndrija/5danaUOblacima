using API.Entities;

namespace API.Repositories
{
    public interface ICanteenRepository
    {
        Task<IEnumerable<Canteen>> GetAllAsync();
        Task<Canteen?> GetByIdAsync(int id);
        Task<Canteen?> GetByIdWithWorkingHoursAsync(int id);
        Task<IEnumerable<Canteen>> GetAllWithWorkingHoursAsync();
        Task<Canteen> AddAsync(Canteen canteen);
        Task<Canteen> UpdateAsync(Canteen canteen);
        Task<bool> DeleteAsync(int id);
        Task<List<Reservation>> GetActiveReservationsByCanteenIdAsync(int canteenId);
        Task<List<Reservation>> GetAllReservationsByCanteenIdAsync(int canteenId);
        Task CancelReservationsAsync(List<Reservation> reservations);
        Task DeleteReservationsAsync(List<Reservation> reservations);
        Task<List<Reservation>> GetReservationsByCanteenDateAndStatusAsync(int canteenId, DateOnly date);
        Task<bool> SaveChangesAsync();
    }
}

using API.Entities;

namespace API.Repositories
{
    public interface IReservationRepository
    {
        Task<IEnumerable<Reservation>> GetAllAsync();
        Task<Reservation?> GetByIdAsync(int id);
        Task<Reservation> AddAsync(Reservation reservation);
        Task<Reservation> UpdateAsync(Reservation reservation);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<List<Reservation>> GetActiveReservationsByStudentAndDateAsync(int studentId, DateOnly date);
        Task<List<Reservation>> GetActiveReservationsByCanteenAndDateAsync(int canteenId, DateOnly date);
        Task<bool> SaveChangesAsync();
    }
}

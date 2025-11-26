using API.Data;
using API.Entities;
using API.Enums;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories
{
    public class ReservationRepository : IReservationRepository
    {
        private readonly AppDbContext _context;

        public ReservationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Reservation>> GetAllAsync()
        {
            return await _context.Reservations.ToListAsync();
        }

        public async Task<Reservation?> GetByIdAsync(int id)
        {
            return await _context.Reservations.FindAsync(id);
        }

        public async Task<Reservation> AddAsync(Reservation reservation)
        {
            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();
            return reservation;
        }

        public async Task<Reservation> UpdateAsync(Reservation reservation)
        {
            _context.Entry(reservation).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return reservation;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return false;
            }

            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Reservations.AnyAsync(e => e.Id == id);
        }

        public async Task<List<Reservation>> GetActiveReservationsByStudentAndDateAsync(int studentId, DateOnly date)
        {
            return await _context.Reservations
                .Where(r => r.StudentId == studentId &&
                            r.Date == date &&
                            r.Status == ReservationStatus.Active)
                .ToListAsync();
        }

        public async Task<List<Reservation>> GetActiveReservationsByCanteenAndDateAsync(int canteenId, DateOnly date)
        {
            return await _context.Reservations
                .Where(r => r.CanteenId == canteenId &&
                            r.Date == date &&
                            r.Status == ReservationStatus.Active)
                .ToListAsync();
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}

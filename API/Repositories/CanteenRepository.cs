using API.Data;
using API.Entities;
using API.Enums;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories
{
    public class CanteenRepository : ICanteenRepository
    {
        private readonly AppDbContext _context;

        public CanteenRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Canteen>> GetAllAsync()
        {
            return await _context.Canteens
                .Include(c => c.WorkingHours)
                .ToListAsync();
        }

        public async Task<Canteen?> GetByIdAsync(int id)
        {
            return await _context.Canteens
                .Include(c => c.WorkingHours)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Canteen> AddAsync(Canteen canteen)
        {
            _context.Canteens.Add(canteen);
            await _context.SaveChangesAsync();
            return canteen;
        }

        public async Task<Canteen> UpdateAsync(Canteen canteen)
        {
            await _context.SaveChangesAsync();
            return canteen;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var canteen = await _context.Canteens.FindAsync(id);
            if (canteen == null)
            {
                return false;
            }

            _context.Canteens.Remove(canteen);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Reservation>> GetAllReservationsByCanteenIdAsync(int canteenId)
        {
            return await _context.Reservations
                .Where(r => r.CanteenId == canteenId)
                .ToListAsync();
        }

        public async Task<List<Reservation>> GetReservationsByCanteenDateAndStatusAsync(int canteenId, DateOnly date)
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

using API.DTOs;
using API.Entities;
using API.Enums;
using API.Repositories;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace API.Services
{
    public class ReservationService : IReservationService
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly ICanteenRepository _canteenRepository;
        private readonly IMapper _mapper;

        public ReservationService(
            IReservationRepository reservationRepository,
            IStudentRepository studentRepository,
            ICanteenRepository canteenRepository,
            IMapper mapper)
        {
            _reservationRepository = reservationRepository;
            _studentRepository = studentRepository;
            _canteenRepository = canteenRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ReservationResponseDto>> GetAllReservationsAsync()
        {
            var reservations = await _reservationRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<ReservationResponseDto>>(reservations);
        }

        public async Task<ReservationResponseDto?> GetReservationByIdAsync(int id)
        {
            var reservation = await _reservationRepository.GetByIdAsync(id);
            if (reservation == null)
            {
                return null;
            }
            return _mapper.Map<ReservationResponseDto>(reservation);
        }

        public async Task<ReservationResponseDto> CreateReservationAsync(ReservationRequestDto reservationDto)
        {
            if (!int.TryParse(reservationDto.StudentId, out var studentId) ||
                !int.TryParse(reservationDto.CanteenId, out var canteenId))
            {
                throw new ArgumentException("Invalid student or canteen ID");
            }

            if (!DateOnly.TryParse(reservationDto.Date, out var date))
            {
                throw new ArgumentException("Invalid date format");
            }

            if (date < DateOnly.FromDateTime(DateTime.Today))
            {
                throw new ArgumentException("Reservation date cannot be in the past");
            }

            if (!TimeOnly.TryParse(reservationDto.Time, out var timeOnly))
            {
                throw new ArgumentException("Invalid time format");
            }

            if (timeOnly.Minute != 0 && timeOnly.Minute != 30)
            {
                throw new ArgumentException("Time must start on the hour or half-hour");
            }

            if (reservationDto.Duration != 30 && reservationDto.Duration != 60)
            {
                throw new ArgumentException("Duration must be 30 or 60 minutes");
            }

            var student = await _studentRepository.GetByIdAsync(studentId);
            if (student == null)
            {
                throw new ArgumentException("Student does not exist");
            }

            var canteen = await _canteenRepository.GetByIdWithWorkingHoursAsync(canteenId);
            if (canteen == null)
            {
                throw new ArgumentException("Canteen does not exist");
            }

            var reservationEnd = timeOnly.AddMinutes(reservationDto.Duration);

            var isWithinWorkingHours = canteen.WorkingHours.Any(wh =>
            {
                var whStart = TimeOnly.Parse(wh.From);
                var whEnd = TimeOnly.Parse(wh.To);

                return timeOnly >= whStart && reservationEnd <= whEnd;
            });

            if (!isWithinWorkingHours)
            {
                throw new InvalidOperationException("Reservation time is outside the canteen's working hours");
            }

            var existingReservations = await _reservationRepository.GetActiveReservationsByStudentAndDateAsync(studentId, date);

            var hasOverlap = existingReservations.Any(existing =>
            {
                var existingStart = TimeOnly.Parse(existing.Time);
                var existingEnd = existingStart.AddMinutes(existing.Duration);

                return existingStart < reservationEnd && existingEnd > timeOnly;
            });

            if (hasOverlap)
            {
                throw new InvalidOperationException("Student already has a reservation at this time");
            }

            var activeReservations = await _reservationRepository.GetActiveReservationsByCanteenAndDateAsync(canteenId, date);

            var overlappingCount = activeReservations.Count(r =>
            {
                var resStart = TimeOnly.Parse(r.Time);
                var resEnd = resStart.AddMinutes(r.Duration);

                return resStart < reservationEnd && resEnd > timeOnly;
            });

            if (overlappingCount >= canteen.Capacity)
            {
                throw new InvalidOperationException("Canteen is at full capacity for this time slot");
            }

            var reservation = _mapper.Map<Reservation>(reservationDto);
            var createdReservation = await _reservationRepository.AddAsync(reservation);

            return _mapper.Map<ReservationResponseDto>(createdReservation);
        }

        public async Task<bool> UpdateReservationAsync(int id, ReservationRequestDto reservationDto)
        {
            if (id <= 0)
            {
                return false;
            }

            var reservation = await _reservationRepository.GetByIdAsync(id);
            if (reservation == null)
            {
                return false;
            }

            _mapper.Map(reservationDto, reservation);

            try
            {
                await _reservationRepository.UpdateAsync(reservation);
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _reservationRepository.ExistsAsync(id))
                {
                    return false;
                }
                throw;
            }
        }

        public async Task<ReservationResponseDto?> CancelReservationAsync(int id, int studentId)
        {
            var reservation = await _reservationRepository.GetByIdAsync(id);

            if (reservation == null)
            {
                return null;
            }

            if (reservation.StudentId != studentId)
            {
                throw new UnauthorizedAccessException("Only the student who made the reservation can cancel it.");
            }

            if (reservation.Status == ReservationStatus.Cancelled)
            {
                throw new InvalidOperationException("Reservation is already cancelled.");
            }

            reservation.Status = ReservationStatus.Cancelled;
            await _reservationRepository.UpdateAsync(reservation);

            return _mapper.Map<ReservationResponseDto>(reservation);
        }

        public async Task<bool> ReservationExistsAsync(int id)
        {
            return await _reservationRepository.ExistsAsync(id);
        }
    }
}

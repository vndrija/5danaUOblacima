using API.DTOs;
using API.Entities;
using API.Enums;
using API.Exceptions;
using API.Repositories;
using AutoMapper;

namespace API.Services
{
    public class CanteenService : ICanteenService
    {
        private readonly ICanteenRepository _canteenRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IMapper _mapper;

        public CanteenService(
            ICanteenRepository canteenRepository,
            IStudentRepository studentRepository,
            IMapper mapper)
        {
            _canteenRepository = canteenRepository;
            _studentRepository = studentRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CanteenResponseDto>> GetAllCanteensAsync()
        {
            var canteens = await _canteenRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<CanteenResponseDto>>(canteens);
        }

        public async Task<CanteenResponseDto?> GetCanteenByIdAsync(int id)
        {
            var canteen = await _canteenRepository.GetByIdAsync(id);
            if (canteen == null)
            {
                return null;
            }
            return _mapper.Map<CanteenResponseDto>(canteen);
        }

        public async Task<CanteenResponseDto> CreateCanteenAsync(CreateCanteenRequestDto canteenDto, int studentId)
        {
            var student = await _studentRepository.GetByIdAsync(studentId);

            if (student == null || !student.IsAdmin)
            {
                throw new ForbiddenException("Only an admin can create a canteen.");
            }

            if (canteenDto.WorkingHours == null || !canteenDto.WorkingHours.Any())
            {
                throw new BadRequestException("Canteen must have working hours.");
            }

            var newCanteen = _mapper.Map<Canteen>(canteenDto);
            var createdCanteen = await _canteenRepository.AddAsync(newCanteen);

            return _mapper.Map<CanteenResponseDto>(createdCanteen);
        }

        public async Task<CanteenResponseDto?> UpdateCanteenAsync(int id, UpdateCanteenRequestDto canteenDto, int studentId)
        {
            if (canteenDto == null)
            {
                throw new BadRequestException("Canteen data must be provided.");
            }

            var student = await _studentRepository.GetByIdAsync(studentId);

            if (student == null || !student.IsAdmin)
            {
                throw new ForbiddenException("Only an admin can update the canteen.");
            }

            var canteen = await _canteenRepository.GetByIdAsync(id);

            if (canteen == null)
            {
                return null;
            }

            _mapper.Map(canteenDto, canteen);

            await _canteenRepository.UpdateAsync(canteen);

            return _mapper.Map<CanteenResponseDto>(canteen);
        }

        public async Task<bool> DeleteCanteenAsync(int id, int studentId)
        {
            var student = await _studentRepository.GetByIdAsync(studentId);

            if (student == null || !student.IsAdmin)
            {
                throw new ForbiddenException("Only an admin can delete the canteen.");
            }

            var canteen = await _canteenRepository.GetByIdAsync(id);

            if (canteen == null)
            {
                return false;
            }

            // Get all active reservations for this canteen and cancel them
            var reservations = await _canteenRepository.GetAllReservationsByCanteenIdAsync(id);
            var activeReservations = reservations.Where(r => r.Status == ReservationStatus.Active).ToList();

            if (activeReservations.Count > 0)
            {
                foreach (var reservation in activeReservations)
                {
                    reservation.Status = ReservationStatus.Cancelled;
                }
                await _canteenRepository.SaveChangesAsync();
            }

            return await _canteenRepository.DeleteAsync(id);
        }

        public async Task<IEnumerable<CanteenStatusResponseDto>> GetCanteensStatusAsync(
            string startDate, string endDate, string startTime, string endTime, int duration)
        {
            if (!DateOnly.TryParse(startDate, out var start) ||
                !DateOnly.TryParse(endDate, out var end) ||
                !TimeOnly.TryParse(startTime, out var timeStart) ||
                !TimeOnly.TryParse(endTime, out var timeEnd))
            {
                throw new BadRequestException("Invalid date or time format.");
            }

            if (start > end || duration <= 0 || (duration != 30 && duration != 60))
            {
                throw new BadRequestException("Invalid input parameters.");
            }

            var canteens = await _canteenRepository.GetAllAsync();
            var response = new List<CanteenStatusResponseDto>();

            foreach (var canteen in canteens)
            {
                var slots = await CalculateSlotsAsync(canteen, start, end, timeStart, timeEnd, duration);

                response.Add(new CanteenStatusResponseDto
                {
                    CanteenId = canteen.Id.ToString(),
                    Slots = slots
                });
            }

            return response;
        }

        public async Task<CanteenStatusResponseDto?> GetCanteenStatusAsync(
            int id, string startDate, string endDate, string startTime, string endTime, int duration)
        {
            if (!DateOnly.TryParse(startDate, out var start) ||
                !DateOnly.TryParse(endDate, out var end) ||
                !TimeOnly.TryParse(startTime, out var timeStart) ||
                !TimeOnly.TryParse(endTime, out var timeEnd))
            {
                throw new BadRequestException("Invalid date or time format.");
            }

            if (start > end || duration <= 0 || (duration != 30 && duration != 60))
            {
                throw new BadRequestException("Invalid input parameters.");
            }

            var canteen = await _canteenRepository.GetByIdAsync(id);

            if (canteen == null)
            {
                return null;
            }

            var slots = await CalculateSlotsAsync(canteen, start, end, timeStart, timeEnd, duration);

            return new CanteenStatusResponseDto
            {
                CanteenId = canteen.Id.ToString(),
                Slots = slots
            };
        }

        private async Task<List<CanteenSlotDto>> CalculateSlotsAsync(
            Canteen canteen, DateOnly start, DateOnly end, TimeOnly timeStart, TimeOnly timeEnd, int duration)
        {
            var slots = new List<CanteenSlotDto>();

            foreach (var workingHour in canteen.WorkingHours)
            {
                var whStart = TimeOnly.Parse(workingHour.From);
                var whEnd = TimeOnly.Parse(workingHour.To);

                var slotStart = (whStart > timeStart) ? whStart : timeStart;
                var slotEnd = (whEnd < timeEnd) ? whEnd : timeEnd;

                if (slotStart >= slotEnd) continue;

                for (var date = start; date <= end; date = date.AddDays(1))
                {
                    var currentTime = slotStart;

                    while (currentTime.AddMinutes(duration) <= slotEnd)
                    {
                        var currentTimeStr = currentTime.ToString("HH:mm");

                        var reservations = await _canteenRepository.GetReservationsByCanteenDateAndStatusAsync(canteen.Id, date);

                        var overlappingCount = reservations.Count(r =>
                        {
                            var resStart = TimeOnly.Parse(r.Time);
                            var resEnd = resStart.AddMinutes(r.Duration);
                            var slotEndTime = currentTime.AddMinutes(duration);

                            return resStart < slotEndTime && resEnd > currentTime;
                        });

                        var remainingCapacity = canteen.Capacity - overlappingCount;

                        slots.Add(new CanteenSlotDto
                        {
                            Date = date.ToString("yyyy-MM-dd"),
                            Meal = workingHour.Meal.ToString().ToLower(),
                            StartTime = currentTimeStr,
                            RemainingCapacity = remainingCapacity
                        });

                        currentTime = currentTime.AddMinutes(duration);
                    }
                }
            }

            return slots;
        }
    }
}

using AutoMapper;
using Kindergarten.BLL.Models.ActivityLogDTO;
using Kindergarten.DAL.Database;
using Kindergarten.DAL.Entity;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.BLL.Services
{
    public class ActivityLogService : IActivityLogService
    {
        #region Prop
        private readonly ApplicationContext _context;
        private readonly IMapper _mapper;
        #endregion

        #region Ctor
        public ActivityLogService(ApplicationContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        #endregion

        #region Methods

        public async Task<int> CreateAsync(ActivityLogCreateDTO dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "Activity log data cannot be null.");

            var entity = _mapper.Map<ActivityLog>(dto);
            entity.PerformedAt = DateTime.UtcNow;

            _context.ActivityLogs.Add(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        public async Task<List<ActivityLogViewDTO>> GetEntityHistoryAsync(string entityName, string entityId)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be empty.", nameof(entityName));

            if (string.IsNullOrWhiteSpace(entityId))
                throw new ArgumentException("Entity ID cannot be empty.", nameof(entityId));

            var logs = await _context.ActivityLogs
                .Where(x => x.EntityName == entityName && x.EntityId == entityId)
                .OrderByDescending(x => x.PerformedAt)
                .ToListAsync();

            return _mapper.Map<List<ActivityLogViewDTO>>(logs);
        }

        public async Task<List<ActivityLogDTO>> GetUserActionsAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));

            var query = _context.ActivityLogs
                .Where(x => x.PerformedByUserId == userId);

            if (fromDate.HasValue)
                query = query.Where(x => x.PerformedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(x => x.PerformedAt <= toDate.Value);

            var logs = await query
                .OrderByDescending(x => x.PerformedAt)
                .ToListAsync();

            return _mapper.Map<List<ActivityLogDTO>>(logs);
        }

        #endregion
    }


    public interface IActivityLogService
    {
        Task<int> CreateAsync(ActivityLogCreateDTO dto);

        Task<List<ActivityLogViewDTO>> GetEntityHistoryAsync(string entityName, string entityId);

        Task<List<ActivityLogDTO>> GetUserActionsAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null);

    }
}

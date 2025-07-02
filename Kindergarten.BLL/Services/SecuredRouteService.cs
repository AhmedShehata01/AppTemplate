using AutoMapper;
using Kindergarten.BLL.Extensions;
using Kindergarten.BLL.Models;
using Kindergarten.BLL.Models.ActivityLogDTO;
using Kindergarten.BLL.Models.DRBRADTO;
using Kindergarten.DAL.Database;
using Kindergarten.DAL.Entity.DRBRA;
using Kindergarten.DAL.Enum;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.BLL.Services
{
    public class SecuredRouteService : ISecuredRouteService
    {
        #region Prop
        private readonly ApplicationContext _context;
        private readonly IMapper _mapper;
        private readonly IActivityLogService _activityLogService;

        #endregion

        #region CTOR
        public SecuredRouteService(ApplicationContext context, IMapper mapper, IActivityLogService activityLogService)
        {
            _context = context;
            _mapper = mapper;
            _activityLogService = activityLogService;
        }
        #endregion

        #region Actions 
        public async Task<PagedResult<SecuredRouteDTO>> GetAllRoutesAsync(PaginationFilter filter)
        {
            var searchText = filter.SearchText?.Trim().ToLower();

            // 📦 بداية الاستعلام من DbSet
            var query = _context.SecuredRoutes
                .Include(r => r.CreatedBy) // لو عايز اسم المستخدم
                .Include(r => r.RoleSecuredRoutes)
                    .ThenInclude(rr => rr.Role)
                .Where(r =>
                    string.IsNullOrEmpty(searchText) ||
                    r.BasePath.ToLower().Contains(searchText) ||
                    r.Description.ToLower().Contains(searchText) ||
                    r.CreatedBy.FullName.ToLower().Contains(searchText)
                );

            // 🌀 ترتيب ديناميكي باستخدام ApplySorting
            query = query.ApplySorting(filter.SortBy, filter.SortDirection);

            // 🔢 إجمالي العناصر
            var totalCount = await query.CountAsync();

            // 📄 تطبيق Pagination
            var pagedData = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .AsNoTracking()
                .ToListAsync();

            // 🗺️ تحويل إلى DTO
            var mapped = _mapper.Map<List<SecuredRouteDTO>>(pagedData);

            // 📤 النتيجة النهائية
            return new PagedResult<SecuredRouteDTO>
            {
                Data = mapped,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<SecuredRouteDTO> GetRouteByIdAsync(int id)
        {
            var route = await _context.SecuredRoutes
                .Include(r => r.RoleSecuredRoutes)
                    .ThenInclude(rr => rr.Role)
                .Include(r => r.CreatedBy)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (route == null) return null;
            return _mapper.Map<SecuredRouteDTO>(route);
        }

        public async Task<int> CreateRouteAsync(CreateSecuredRouteDTO dto, string? performedByUserId, string? performedByUserName)
        {
            var route = new SecuredRoute
            {
                BasePath = dto.BasePath,
                Description = dto.Description,
                CreatedById = performedByUserId,
                CreatedOn = DateTime.Now,
                RoleSecuredRoutes = dto.RoleIds?.Select(roleId => new RoleSecuredRoute
                {
                    RoleId = roleId
                }).ToList()
            };

            _context.SecuredRoutes.Add(route);
            await _context.SaveChangesAsync();

            // سجل ActivityLog لتسجيل إنشاء الراوت
            var assignedRolesJson = System.Text.Json.JsonSerializer.Serialize(dto.RoleIds ?? new List<string>());
            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = "SecuredRoute",
                EntityId = route.Id.ToString(),
                ActionType = ActivityActionType.Created,
                SystemComment = $"تم إنشاء مسار مؤمن جديد: {route.BasePath}.",
                PerformedByUserId = performedByUserId,
                PerformedByUserName = performedByUserName,
                OldValues = null,
                NewValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    route.BasePath,
                    route.Description,
                    AssignedRoles = assignedRolesJson
                })
            });

            return route.Id;
        }


        public async Task<bool> UpdateRouteAsync(UpdateSecuredRouteDTO dto, string? performedByUserId, string? performedByUserName)
        {
            var existing = await _context.SecuredRoutes
                .Include(r => r.RoleSecuredRoutes)
                .FirstOrDefaultAsync(r => r.Id == dto.Id);

            if (existing == null) return false;

            // تأكد مفيش تعارض في BasePath
            if (await _context.SecuredRoutes.AnyAsync(r => r.Id != dto.Id && r.BasePath == dto.BasePath))
                throw new Exception("Another route already uses the same base path.");

            // خذ نسخة من القيم القديمة (قبل التحديث)
            var oldValues = new
            {
                existing.BasePath,
                existing.Description,
                RoleIds = existing.RoleSecuredRoutes.Select(rr => rr.RoleId).ToList()
            };
            var oldValuesJson = System.Text.Json.JsonSerializer.Serialize(oldValues);

            // ✏️ تحديث البيانات
            existing.BasePath = dto.BasePath;
            existing.Description = dto.Description;

            // 🔄 تحديث الأدوار: حذف القديمة وإضافة الجديدة
            _context.RoleSecuredRoutes.RemoveRange(existing.RoleSecuredRoutes);

            if (dto.RoleIds != null && dto.RoleIds.Any())
            {
                existing.RoleSecuredRoutes = dto.RoleIds.Select(roleId => new RoleSecuredRoute
                {
                    RoleId = roleId,
                    SecuredRouteId = existing.Id
                }).ToList();
            }

            await _context.SaveChangesAsync();

            // سجل القيم الجديدة (بعد التحديث)
            var newValues = new
            {
                dto.BasePath,
                dto.Description,
                RoleIds = dto.RoleIds ?? new List<string>()
            };
            var newValuesJson = System.Text.Json.JsonSerializer.Serialize(newValues);

            // إنشاء سجل النشاط
            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = "SecuredRoute",
                EntityId = existing.Id.ToString(),
                ActionType = ActivityActionType.Updated,
                SystemComment = $"تم تحديث مسار مؤمن: {existing.BasePath}.",
                PerformedByUserId = performedByUserId,
                PerformedByUserName = performedByUserName,
                OldValues = oldValuesJson,
                NewValues = newValuesJson
            });

            return true;
        }


        public async Task<bool> DeleteRouteAsync(int id, string? performedByUserId, string? performedByUserName)
        {
            var entity = await _context.SecuredRoutes
                .Include(r => r.RoleSecuredRoutes)
                .ThenInclude(rr => rr.Role)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (entity == null) return false;

            // تجهيز بيانات قديمة لتسجيلها في ال ActivityLog
            var oldValues = new
            {
                entity.Id,
                entity.BasePath,
                entity.Description,
                RoleIds = entity.RoleSecuredRoutes.Select(rr => rr.RoleId).ToList()
            };
            var oldValuesJson = System.Text.Json.JsonSerializer.Serialize(oldValues);

            // إزالة العلاقات المرتبطة
            _context.RoleSecuredRoutes.RemoveRange(entity.RoleSecuredRoutes);
            _context.SecuredRoutes.Remove(entity);

            await _context.SaveChangesAsync();

            // تسجيل الحدث
            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = "SecuredRoute",
                EntityId = id.ToString(),
                ActionType = ActivityActionType.Deleted,
                SystemComment = $"تم حذف مسار مؤمن: {entity.BasePath}.",
                PerformedByUserId = performedByUserId,
                PerformedByUserName = performedByUserName,
                OldValues = oldValuesJson,
                NewValues = null
            });

            return true;
        }


        public async Task<bool> AssignRolesAsync(AssignRolesToRouteDTO dto, string? performedByUserId, string? performedByUserName)
        {
            var route = await _context.SecuredRoutes
                .Include(r => r.RoleSecuredRoutes)
                .ThenInclude(rr => rr.Role)
                .FirstOrDefaultAsync(r => r.Id == dto.SecuredRouteId);
            if (route == null) return false;

            var existingRoleIds = route.RoleSecuredRoutes.Select(r => r.RoleId).ToHashSet();
            var newRoleIds = dto.RoleIds.Except(existingRoleIds).ToList();

            if (!newRoleIds.Any())
                return true;

            foreach (var roleId in newRoleIds)
            {
                route.RoleSecuredRoutes.Add(new RoleSecuredRoute
                {
                    SecuredRouteId = dto.SecuredRouteId,
                    RoleId = roleId
                });
            }

            await _context.SaveChangesAsync();

            // تسجيل الـ ActivityLog لل Roles الجديدة المُضافة
            var addedRolesNames = await _context.Roles
                .Where(r => newRoleIds.Contains(r.Id))
                .Select(r => r.Name)
                .ToListAsync();

            var newValuesJson = System.Text.Json.JsonSerializer.Serialize(addedRolesNames);

            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = "SecuredRouteRoles",
                EntityId = dto.SecuredRouteId.ToString(),
                ActionType = ActivityActionType.Updated,
                SystemComment = $"تم إضافة الأدوار: {string.Join(", ", addedRolesNames)} للمسار المؤمّن: {route.BasePath}.",
                PerformedByUserId = performedByUserId,
                PerformedByUserName = performedByUserName,
                OldValues = null,
                NewValues = newValuesJson
            });

            return true;
        }


        public async Task<bool> UnassignRoleAsync(UnassignRoleFromRouteDTO dto, string? performedByUserId, string? performedByUserName)
        {
            var record = await _context.RoleSecuredRoutes
                .Include(r => r.Role)
                .Include(r => r.SecuredRoute)
                .FirstOrDefaultAsync(r => r.SecuredRouteId == dto.SecuredRouteId && r.RoleId == dto.RoleId);
            if (record == null) return false;

            _context.RoleSecuredRoutes.Remove(record);
            await _context.SaveChangesAsync();

            var roleName = record.Role?.Name ?? "Unknown Role";
            var routePath = record.SecuredRoute?.BasePath ?? "Unknown Route";

            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = "SecuredRouteRoles",
                EntityId = dto.SecuredRouteId.ToString(),
                ActionType = ActivityActionType.Updated,
                SystemComment = $"تم إزالة الدور '{roleName}' من المسار المؤمّن '{routePath}'.",
                PerformedByUserId = performedByUserId,
                PerformedByUserName = performedByUserName,
                OldValues = System.Text.Json.JsonSerializer.Serialize(roleName),
                NewValues = null
            });

            return true;
        }


        public async Task<List<RouteWithRolesDTO>> GetRoutesWithRolesAsync()
        {
            var routes = await _context.SecuredRoutes
                .Include(r => r.RoleSecuredRoutes)
                    .ThenInclude(rr => rr.Role)
                .ToListAsync();

            return _mapper.Map<List<RouteWithRolesDTO>>(routes);
        }

        public async Task<bool> IsRoleAssignedToAnySecuredRouteAsync(string roleId)
        {
            return await _context.RoleSecuredRoutes
                                 .AnyAsync(r => r.RoleId == roleId);
        }

        #endregion


    }



    public interface ISecuredRouteService
    {
        Task<PagedResult<SecuredRouteDTO>> GetAllRoutesAsync(PaginationFilter filter);
        Task<SecuredRouteDTO> GetRouteByIdAsync(int id);
        Task<int> CreateRouteAsync(CreateSecuredRouteDTO dto, string? performedByUserId, string? performedByUserName);
        Task<bool> UpdateRouteAsync(UpdateSecuredRouteDTO dto, string? performedByUserId, string? performedByUserName);
        Task<bool> DeleteRouteAsync(int id, string? performedByUserId, string? performedByUserName);
        Task<bool> AssignRolesAsync(AssignRolesToRouteDTO dto, string? performedByUserId, string? performedByUserName);
        Task<bool> UnassignRoleAsync(UnassignRoleFromRouteDTO dto, string? performedByUserId, string? performedByUserName);
        Task<List<RouteWithRolesDTO>> GetRoutesWithRolesAsync();
        Task<bool> IsRoleAssignedToAnySecuredRouteAsync(string roleId);

    }
}

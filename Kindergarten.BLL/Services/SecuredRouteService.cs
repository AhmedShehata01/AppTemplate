using AutoMapper;
using Kindergarten.BLL.Extensions;
using Kindergarten.BLL.Models;
using Kindergarten.BLL.Models.ActivityLogDTO;
using Kindergarten.BLL.Models.DRBRADTO;
using Kindergarten.DAL.Database;
using Kindergarten.DAL.Entity.DRBRA;
using Kindergarten.DAL.Enum;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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

            if (route == null)
                throw new KeyNotFoundException($"المسار المؤمّن بالمعرّف {id} غير موجود.");

            return _mapper.Map<SecuredRouteDTO>(route);
        }

        public async Task<int> CreateRouteAsync(CreateSecuredRouteDTO dto, string? performedByUserId, string? performedByUserName)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

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
            // 1. تحقق من صحة الـ DTO
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "بيانات التحديث غير صحيحة.");

            // 2. جلب المسار مع الأدوار المرتبطة
            var existing = await _context.SecuredRoutes
                .Include(r => r.RoleSecuredRoutes)
                .FirstOrDefaultAsync(r => r.Id == dto.Id);

            // 3. إذا المسار مش موجود → KeyNotFoundException
            if (existing == null)
                throw new KeyNotFoundException($"المسار المؤمّن بالمعرّف {dto.Id} غير موجود.");

            // 4. تأكد من عدم وجود مسار آخر بنفس BasePath
            bool conflict = await _context.SecuredRoutes
                .AnyAsync(r => r.Id != dto.Id && r.BasePath == dto.BasePath);
            if (conflict)
                throw new InvalidOperationException($"يوجد مسار آخر يستخدم BasePath '{dto.BasePath}'.");


            // 5. حفظ قيم قديمة للتوثيق
            var oldValues = new
            {
                existing.BasePath,
                existing.Description,
                RoleIds = existing.RoleSecuredRoutes.Select(rr => rr.RoleId).ToList()
            };
            var oldValuesJson = JsonSerializer.Serialize(oldValues);

            // 6. تحديث بيانات المسار
            existing.BasePath = dto.BasePath;
            existing.Description = dto.Description;


            // 7. إعادة تعيين الأدوار
            _context.RoleSecuredRoutes.RemoveRange(existing.RoleSecuredRoutes);
            if (dto.RoleIds != null && dto.RoleIds.Any())
            {
                existing.RoleSecuredRoutes = dto.RoleIds
                    .Select(roleId => new RoleSecuredRoute
                    {
                        SecuredRouteId = existing.Id,
                        RoleId = roleId
                    })
                    .ToList();
            }

            await _context.SaveChangesAsync();

            // 8. حفظ القيم الجديدة للتوثيق
            var newValues = new
            {
                dto.BasePath,
                dto.Description,
                RoleIds = dto.RoleIds ?? new List<string>()
            };
            var newValuesJson = JsonSerializer.Serialize(newValues);

            // 9. تسجيل نشاط التحديث
            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = "SecuredRoute",
                EntityId = existing.Id.ToString(),
                ActionType = ActivityActionType.Updated,
                SystemComment = $"تم تحديث مسار مؤمّن: {existing.BasePath}.",
                PerformedByUserId = performedByUserId,
                PerformedByUserName = performedByUserName,
                OldValues = oldValuesJson,
                NewValues = newValuesJson
            });

            return true;
        }


        public async Task<bool> DeleteRouteAsync(int id, string? performedByUserId, string? performedByUserName)
        {
            // 1. جلب الكيان مع العلاقات
            var entity = await _context.SecuredRoutes
                .Include(r => r.RoleSecuredRoutes)
                    .ThenInclude(rr => rr.Role)
                .FirstOrDefaultAsync(r => r.Id == id);

            // 2. لو مش موجود → نرمي KeyNotFoundException
            if (entity == null)
                throw new KeyNotFoundException($"المسار المؤمّن بالمعرّف {id} غير موجود.");

            // 3. تجهيز بيانات قديمة للتوثيق
            var oldValues = new
            {
                entity.Id,
                entity.BasePath,
                entity.Description,
                RoleIds = entity.RoleSecuredRoutes.Select(rr => rr.RoleId).ToList()
            };
            var oldValuesJson = JsonSerializer.Serialize(oldValues);

            // 4. حذف العلاقات والكيان
            _context.RoleSecuredRoutes.RemoveRange(entity.RoleSecuredRoutes);
            _context.SecuredRoutes.Remove(entity);

            await _context.SaveChangesAsync();

            // 5. تسجيل نشاط الحذف
            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = "SecuredRoute",
                EntityId = id.ToString(),
                ActionType = ActivityActionType.Deleted,
                SystemComment = $"تم حذف مسار مؤمّن: {entity.BasePath}.",
                PerformedByUserId = performedByUserId,
                PerformedByUserName = performedByUserName,
                OldValues = oldValuesJson,
                NewValues = null
            });

            return true;
        }


        public async Task<bool> AssignRolesAsync(AssignRolesToRouteDTO dto, string? performedByUserId, string? performedByUserName)
        {
            // 1. التحقق من صحة الـ DTO
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "بيانات تعيين الأدوار غير صحيحة.");


            // 2. جلب المسار مع العلاقات
            var route = await _context.SecuredRoutes
                .Include(r => r.RoleSecuredRoutes)
                .ThenInclude(rr => rr.Role)
                .FirstOrDefaultAsync(r => r.Id == dto.SecuredRouteId);

            // 3. إذا المسار غير موجود → KeyNotFoundException
            if (route == null)
                throw new KeyNotFoundException($"المسار المؤمّن بالمعرّف {dto.SecuredRouteId} غير موجود.");

            // 4. احصل على الأدوار الجديدة فقط
            var existingRoleIds = route.RoleSecuredRoutes.Select(r => r.RoleId).ToHashSet();
            var newRoleIds = dto.RoleIds!.Except(existingRoleIds).ToList();

            // 5. إذا لا توجد أدوار جديدة → OK
            if (!newRoleIds.Any())
                return true;

            // 6. أضف الأدوار الجديدة
            foreach (var roleId in newRoleIds)
            {
                route.RoleSecuredRoutes.Add(new RoleSecuredRoute
                {
                    SecuredRouteId = dto.SecuredRouteId,
                    RoleId = roleId
                });
            }

            await _context.SaveChangesAsync();

            // 7. سجل أسماء الأدوار الجديدة للتوثيق
            var addedRolesNames = await _context.Roles
                .Where(r => newRoleIds.Contains(r.Id))
                .Select(r => r.Name)
                .ToListAsync();

            var newValuesJson = JsonSerializer.Serialize(addedRolesNames);


            // 8. تسجيل النشاط
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
            // 1. تحقق من صحة الـ DTO
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "بيانات إلغاء تعيين الدور غير صحيحة.");

            // 2. جلب السجل
            var record = await _context.RoleSecuredRoutes
                .Include(r => r.Role)
                .Include(r => r.SecuredRoute)
                .FirstOrDefaultAsync(r =>
                    r.SecuredRouteId == dto.SecuredRouteId &&
                    r.RoleId == dto.RoleId);

            // 3. إذا السجل غير موجود → KeyNotFoundException
            if (record == null)
                throw new KeyNotFoundException(
                    $"لا توجد علاقة بين المسار '{dto.SecuredRouteId}' والدور '{dto.RoleId}'.");

            // 4. إزالة السجل وحفظ التغييرات
            _context.RoleSecuredRoutes.Remove(record);
            await _context.SaveChangesAsync();

            // 5. تحضير البيانات للاحتفاظ بالسجل
            var roleName = record.Role?.Name ?? "Unknown Role";
            var routePath = record.SecuredRoute?.BasePath ?? "Unknown Route";

            // 6. تسجيل النشاط
            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = "SecuredRouteRoles",
                EntityId = dto.SecuredRouteId.ToString(),
                ActionType = ActivityActionType.Updated,
                SystemComment = $"تم إزالة الدور '{roleName}' من المسار المؤمّن '{routePath}'.",
                PerformedByUserId = performedByUserId,
                PerformedByUserName = performedByUserName,
                OldValues = JsonSerializer.Serialize(roleName),
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

            if (!routes.Any())
                throw new KeyNotFoundException("لا توجد مسارات مؤمنة.");


            return _mapper.Map<List<RouteWithRolesDTO>>(routes);
        }

        public async Task<bool> IsRoleAssignedToAnySecuredRouteAsync(string roleId)
        {
            if (string.IsNullOrWhiteSpace(roleId))
                throw new ArgumentNullException(nameof(roleId), "معرّف الدور مطلوب.");

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

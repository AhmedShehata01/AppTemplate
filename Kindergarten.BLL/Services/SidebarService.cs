using System.Text.Json;
using AutoMapper;
using Kindergarten.BLL.Models;
using Kindergarten.BLL.Models.ActivityLogDTO;
using Kindergarten.BLL.Models.DRBRADTO;
using Kindergarten.DAL.Database;
using Kindergarten.DAL.Entity.DRBRA;
using Kindergarten.DAL.Enum;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.BLL.Services
{
    public class SidebarService : ISidebarService
    {
        #region prop
        private readonly ApplicationContext _db;
        private readonly IMapper _mapper;
        private readonly IActivityLogService _activityLogService;
        #endregion

        #region CTOR
        public SidebarService(ApplicationContext db, IMapper mapper, IActivityLogService activityLogService)
        {
            _db = db;
            _mapper = mapper;
            _activityLogService = activityLogService;
        }
        #endregion

        #region Actions 
        public async Task<PagedResult<SidebarItemDTO>> GetPagedAsync(PaginationFilter filter)
        {
            var query = _db.SidebarItem
                .Where(x => !x.IsDeleted && x.ParentId == null)
                .Include(x => x.Children.Where(c => !c.IsDeleted))
                .AsQueryable();

            // 🔍 Search
            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var search = filter.SearchText.Trim().ToLower();
                query = query.Where(x =>
                        x.LabelAr.ToLower().Contains(search) ||
                        x.LabelEn.ToLower().Contains(search));
            }

            // 🔃 Sorting
            if (!string.IsNullOrWhiteSpace(filter.SortBy))
            {
                var isDesc = filter.SortDirection?.ToLower() == "desc";

                switch (filter.SortBy.ToLower())
                {
                    case "labelar":
                        query = isDesc ? query.OrderByDescending(x => x.LabelAr) : query.OrderBy(x => x.LabelAr);
                        break;

                    case "labelen":
                        query = isDesc ? query.OrderByDescending(x => x.LabelEn) : query.OrderBy(x => x.LabelEn);
                        break;

                    case "route":
                        query = isDesc ? query.OrderByDescending(x => x.Route) : query.OrderBy(x => x.Route);
                        break;

                    case "icon":
                        query = isDesc ? query.OrderByDescending(x => x.Icon) : query.OrderBy(x => x.Icon);
                        break;

                    case "order":
                        query = isDesc ? query.OrderByDescending(x => x.Order) : query.OrderBy(x => x.Order);
                        break;
                    default:
                        query = query.OrderBy(x => x.Order); // default
                        break;
                }
            }
            else
            {
                query = query.OrderBy(x => x.Order);
            }

            // 📊 Pagination
            var totalCount = await query.CountAsync();
            var data = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var result = new PagedResult<SidebarItemDTO>
            {
                Data = _mapper.Map<List<SidebarItemDTO>>(data),
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };

            return result;
        }


        public async Task<SidebarItemDTO> GetByIdAsync(int id)
        {
            var item = await _db.SidebarItem
                .Include(x => x.Children.Where(c => !c.IsDeleted))
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (item == null)
                throw new KeyNotFoundException($"عنصر القائمة الجانبية بالمعرّف {id} غير موجود.");

            return _mapper.Map<SidebarItemDTO>(item);
        }


        public async Task<int> CreateAsync(CreateSidebarItemDTO dto, string? performedByUserId, string? performedByUserName)
        {
            // 1. التحقق من صحة الـ DTO
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "بيانات إنشاء عنصر القائمة الجانبية غير صحيحة.");

            // 2. تحويل الـ DTO إلى كيان وحفظه
            var entity = _mapper.Map<SidebarItem>(dto);
            await _db.SidebarItem.AddAsync(entity);
            await _db.SaveChangesAsync();

            // 3. تسجيل الـ ActivityLog
            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = "SidebarItem",
                EntityId = entity.Id.ToString(),
                ActionType = ActivityActionType.Created,
                SystemComment = $"تم إنشاء عنصر القائمة الجانبية '{entity.LabelAr}'.",
                PerformedByUserId = performedByUserId,
                PerformedByUserName = performedByUserName,
                OldValues = null,
                NewValues = JsonSerializer.Serialize(entity)
            });

            return entity.Id;
        }



        public async Task<bool> UpdateAsync(UpdateSidebarItemDTO dto, string? performedByUserId, string? performedByUserName)
        {
            // 1. تحقق من صحة الـ DTO
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "بيانات التحديث غير صحيحة.");

            // 2. جلب العنصر
            var item = await _db.SidebarItem.FindAsync(dto.Id);
            if (item == null || item.IsDeleted)
                throw new KeyNotFoundException($"عنصر القائمة الجانبية بالمعرّف {dto.Id} غير موجود.");

            // 3. حفظ القيم القديمة للتوثيق
            var oldValuesJson = JsonSerializer.Serialize(item);

            // 4. تطبيق التحديثات
            _mapper.Map(dto, item);
            await _db.SaveChangesAsync();

            // 5. حفظ القيم الجديدة للتوثيق
            var newValuesJson = JsonSerializer.Serialize(item);

            // 6. تسجيل النشاط
            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = "SidebarItem",
                EntityId = item.Id.ToString(),
                ActionType = ActivityActionType.Updated,
                SystemComment = $"تم تعديل عنصر القائمة الجانبية '{item.LabelAr}'.",
                PerformedByUserId = performedByUserId,
                PerformedByUserName = performedByUserName,
                OldValues = oldValuesJson,
                NewValues = newValuesJson
            });

            return true;
        }



        public async Task<bool> SoftDeleteAsync(int id, string? performedByUserId, string? performedByUserName)
        {
            // 1. جلب العنصر مع الأطفال
            var item = await _db.SidebarItem
                .Include(x => x.Children)
                .FirstOrDefaultAsync(x => x.Id == id);

            // 2. إذا العنصر غير موجود أو محذوف مسبقًا → استثناء
            if (item == null || item.IsDeleted)
                throw new KeyNotFoundException($"عنصر القائمة الجانبية بالمعرّف {id} غير موجود أو محذوف بالفعل.");

            // 3. حفظ القيم القديمة للتوثيق
            var oldValuesJson = JsonSerializer.Serialize(new
            {
                item.Id,
                item.LabelAr,
                item.IsDeleted,
                Children = item.Children.Select(c => new { c.Id, c.LabelAr, c.IsDeleted })
            });

            // 4. تطبيق الحذف الناعم على العنصر والأطفال
            item.IsDeleted = true;
            foreach (var child in item.Children)
            {
                child.IsDeleted = true;
            }
            await _db.SaveChangesAsync();

            // 5. تسجيل الـ Activity Log
            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = "SidebarItem",
                EntityId = item.Id.ToString(),
                ActionType = ActivityActionType.Deleted,
                SystemComment = $"تم حذف عنصر القائمة الجانبية '{item.LabelAr}' (حذف ناعم).",
                PerformedByUserId = performedByUserId,
                PerformedByUserName = performedByUserName,
                OldValues = oldValuesJson,
                NewValues = null
            });

            return true;
        }



        public async Task<List<SidebarItemDTO>> GetParentItemsAsync()
        {
            var parents = await _db.SidebarItem
                .Where(x => x.ParentId == null && !x.IsDeleted)
                .OrderBy(x => x.Order)
                .ToListAsync();

            return _mapper.Map<List<SidebarItemDTO>>(parents);
        }

        #endregion
    }

    public interface ISidebarService
    {
        Task<PagedResult<SidebarItemDTO>> GetPagedAsync(PaginationFilter filter);
        Task<SidebarItemDTO?> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateSidebarItemDTO dto, string? performedByUserId, string? performedByUserName);
        Task<bool> UpdateAsync(UpdateSidebarItemDTO dto, string? performedByUserId, string? performedByUserName);
        Task<bool> SoftDeleteAsync(int id, string? performedByUserId, string? performedByUserName);
        Task<List<SidebarItemDTO>> GetParentItemsAsync();
    }

}

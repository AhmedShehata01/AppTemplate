using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Kindergarten.BLL.Extensions;
using Kindergarten.BLL.Models;
using Kindergarten.BLL.Models.ActivityLogDTO;
using Kindergarten.BLL.Models.BranchDTO;
using Kindergarten.BLL.Models.KindergartenDTO;
using Kindergarten.BLL.Repository;
using Kindergarten.DAL.Database;
using Kindergarten.DAL.Entity;
using Kindergarten.DAL.Enum;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.BLL.Services
{
    public class KindergartenService : IKindergartenService
    {
        #region Prop
        private readonly IGenericRepository<KG, int> _kgRepository;
        private readonly IMapper _mapper;
        private readonly IActivityLogService _activityLogService;

        public ApplicationContext _db { get; }
        #endregion

        #region CTOR
        public KindergartenService(ApplicationContext db,
                                    IGenericRepository<KG, int> kgRepository,
                                    IMapper mapper,
                                    IActivityLogService activityLogService)
        {
            _db = db;
            _kgRepository = kgRepository;
            _mapper = mapper;
            _activityLogService = activityLogService;
        }
        #endregion

        #region Actions
        public async Task<PagedResult<KindergartenDTO>> GetAllKgsAsync(PaginationFilter filter)
        {

            var searchText = filter.SearchText?.Trim(); // ✅ نستخدم نسخة معالجة

            Expression<Func<KG, bool>> where = k =>
                k.IsDeleted == false &&
                (
                    string.IsNullOrEmpty(searchText) ||
                    k.NameAr.Contains(searchText) ||
                    k.NameEn.Contains(searchText) ||
                    k.KGCode.Contains(searchText) ||
                    k.Address.Contains(searchText) ||
                    k.Branches.Any(b =>
                        b.IsDeleted == false &&
                        (
                            b.NameAr.Contains(searchText) ||
                            b.NameEn.Contains(searchText) ||
                            b.Address.Contains(searchText) ||
                            b.Phone.Contains(searchText) ||
                            b.Email.Contains(searchText) ||
                            b.BranchCode.Contains(searchText)
                        )
                    )
                );

            Func<IQueryable<KG>, IOrderedQueryable<KG>>? orderBy = query =>
                query.ApplySorting(filter.SortBy, filter.SortDirection);

            var result = await _kgRepository.GetAsync(
                filter: where,
                page: filter.Page,
                pageSize: filter.PageSize,
                orderBy: orderBy,
                includeProperties: new List<Expression<Func<KG, object>>> { k => k.Branches },
                noTrack: true
            );

            var totalCount = await _kgRepository.CountAsync(where);

            return new PagedResult<KindergartenDTO>
            {
                Data = _mapper.Map<List<KindergartenDTO>>(result),
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }


        public async Task<KindergartenDTO> GetKgByIdAsync(int id)
        {
            var kg = await _db.Kindergartens
                .Include(k => k.Branches)
                .FirstOrDefaultAsync(k => k.Id == id);

            return _mapper.Map<KindergartenDTO>(kg);
        }

        public async Task<KindergartenDTO> CreateKgAsync(KindergartenCreateDTO dto, string userId, string userName)
        {
            var kg = _mapper.Map<KG>(dto);
            kg.KGCode = await GenerateKgCodeAsync();
            kg.CreatedBy = userId;
            kg.Branches = new List<Branch>();

            if (dto.Branches == null || !dto.Branches.Any())
                throw new InvalidOperationException("A kindergarten must have at least one branch.");

            var lastCode = await _db.Branches
                .OrderByDescending(b => b.BranchCode)
                .Select(b => b.BranchCode)
                .FirstOrDefaultAsync();

            int nextCodeNumber = 1;
            if (!string.IsNullOrEmpty(lastCode) && lastCode.Length == 8 && lastCode.StartsWith("BR"))
            {
                var numericPart = lastCode.Substring(2);
                if (int.TryParse(numericPart, out int lastNumber))
                {
                    nextCodeNumber = lastNumber + 1;
                }
            }

            var usedCodesInCurrentRequest = new HashSet<string>();

            foreach (var branchDto in dto.Branches)
            {
                if (branchDto.Id > 0)
                    continue;

                var branch = _mapper.Map<Branch>(branchDto);
                branch.CreatedBy = userId;

                string newBranchCode;
                do
                {
                    newBranchCode = $"BR{nextCodeNumber.ToString("D6")}";
                    nextCodeNumber++;
                }
                while (usedCodesInCurrentRequest.Contains(newBranchCode));

                usedCodesInCurrentRequest.Add(newBranchCode);
                branch.BranchCode = newBranchCode;

                kg.Branches.Add(branch);
            }

            try
            {
                var result = await _kgRepository.AddAsync(kg);

                // ✨ Logging بعد حفظ الكيان ووجود Id حقيقي
                var newKgDto = _mapper.Map<KindergartenDTO>(result);
                var newData = System.Text.Json.JsonSerializer.Serialize(newKgDto);

                var entityType = _db.Model.FindEntityType(typeof(KG));
                var tableName = entityType?.GetTableName() ?? nameof(KG);

                var logDto = new ActivityLogCreateDTO
                {
                    EntityName = tableName,
                    EntityId = result.Id.ToString(),
                    ActionType = ActivityActionType.Created,
                    NewValues = newData,
                    PerformedByUserId = userId,
                    PerformedByUserName = userName,
                    SystemComment = "إضافة حضانة جديدة مع الفروع"
                };

                await _activityLogService.CreateAsync(logDto);

                return _mapper.Map<KindergartenDTO>(result);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Branches_BranchCode") == true)
            {
                throw new Exception("An error occurred: A branch code already exists. Please try again.");
            }
        }


        public async Task<KindergartenDTO> UpdateKgAsync(KindergartenUpdateDTO dto, string userId, string userName, string? userComment)
        {
            // جلب الحضانة الأصلية مع الفروع
            var existingKg = await _db.Kindergartens
                .Include(k => k.Branches)
                .FirstOrDefaultAsync(k => k.Id == dto.Id);

            if (existingKg == null)
                return null;

            // ⬅️ لقطة من البيانات القديمة قبل التعديل
            var oldKgDto = _mapper.Map<KindergartenDTO>(existingKg);
            var oldData = System.Text.Json.JsonSerializer.Serialize(oldKgDto);

            // تحديث بيانات الحضانة
            existingKg.NameAr = dto.NameAr;
            existingKg.NameEn = dto.NameEn;
            existingKg.Address = dto.Address;
            existingKg.UpdatedBy = userId;
            existingKg.UpdatedOn = DateTime.UtcNow;

            dto.Branches ??= new List<BranchUpdateDTO>();

            var incomingBranchIds = dto.Branches.Where(b => b.Id > 0).Select(b => b.Id).ToList();

            // Soft delete للفروع المحذوفة
            foreach (var branch in existingKg.Branches
                .Where(b => !incomingBranchIds.Contains(b.Id) && b.IsDeleted == false))
            {
                branch.IsDeleted = true;
                branch.UpdatedBy = userId;
                branch.UpdatedOn = DateTime.UtcNow;
            }

            var usedCodes = new HashSet<string>(existingKg.Branches.Select(b => b.BranchCode));

            int nextCodeNumber = 1;
            var lastCode = await _db.Branches
                .OrderByDescending(b => b.BranchCode)
                .Select(b => b.BranchCode)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(lastCode) && lastCode.StartsWith("BR") && lastCode.Length == 8)
            {
                if (int.TryParse(lastCode.Substring(2), out int lastNum))
                    nextCodeNumber = lastNum + 1;
            }

            foreach (var branchDto in dto.Branches)
            {
                if (branchDto.Id > 0)
                {
                    var existingBranch = existingKg.Branches.FirstOrDefault(b => b.Id == branchDto.Id);
                    if (existingBranch != null)
                    {
                        existingBranch.NameAr = branchDto.NameAr;
                        existingBranch.NameEn = branchDto.NameEn;
                        existingBranch.Address = branchDto.Address;
                        existingBranch.Phone = branchDto.Phone;
                        existingBranch.Email = branchDto.Email;
                        existingBranch.UpdatedBy = userId;
                        existingBranch.UpdatedOn = DateTime.UtcNow;
                        existingBranch.IsDeleted = false;
                    }
                }
                else
                {
                    var newBranch = new Branch
                    {
                        NameAr = branchDto.NameAr,
                        NameEn = branchDto.NameEn,
                        Address = branchDto.Address,
                        Phone = branchDto.Phone,
                        Email = branchDto.Email,
                        CreatedBy = userId,
                        CreatedOn = DateTime.UtcNow,
                        IsDeleted = false,
                        KindergartenId = existingKg.Id
                    };

                    string branchCode;
                    do
                    {
                        branchCode = $"BR{nextCodeNumber.ToString("D6")}";
                        nextCodeNumber++;
                    } while (usedCodes.Contains(branchCode));

                    usedCodes.Add(branchCode);
                    newBranch.BranchCode = branchCode;

                    existingKg.Branches.Add(newBranch);
                }
            }

            if (!existingKg.Branches.Any(b => b.IsDeleted == false))
                throw new InvalidOperationException("Each kindergarten must have at least one active branch.");

            await _db.SaveChangesAsync();

            // Logging بعد التحديث
            var updatedKgDto = _mapper.Map<KindergartenDTO>(existingKg);
            var newData = System.Text.Json.JsonSerializer.Serialize(updatedKgDto);

            var entityType = _db.Model.FindEntityType(typeof(KG));
            var tableName = entityType?.GetTableName() ?? nameof(KG);

            var logDto = new ActivityLogCreateDTO
            {
                EntityName = tableName,
                EntityId = existingKg.Id.ToString(),
                ActionType = ActivityActionType.Updated,
                OldValues = oldData,            // ✅ أضفنا الـ OldValues هنا
                NewValues = newData,
                PerformedByUserId = userId,
                PerformedByUserName = userName,
                SystemComment = "تحديث بيانات حضانة مع الفروع",
                UserComment = userComment
            };

            await _activityLogService.CreateAsync(logDto);

            return updatedKgDto;
        }


        public async Task<bool> DeleteKgAsync(int id, string userId, string userName, string? userComment)
        {
            // نجيب بيانات الحضانة مع الفروع قبل الحذف
            var kg = await _db.Kindergartens
                .Include(k => k.Branches)
                .FirstOrDefaultAsync(k => k.Id == id);

            if (kg == null)
                return false;

            // ✨ نسجل Old Values
            var oldKgDto = _mapper.Map<KindergartenDTO>(kg);
            var oldData = System.Text.Json.JsonSerializer.Serialize(oldKgDto);

            // تنفيذ الحذف الفعلي
            _db.Kindergartens.Remove(kg);
            await _db.SaveChangesAsync();

            // ✨ إنشاء Log
            var entityType = _db.Model.FindEntityType(typeof(KG));
            var tableName = entityType?.GetTableName() ?? nameof(KG);

            var logDto = new ActivityLogCreateDTO
            {
                EntityName = tableName,
                EntityId = kg.Id.ToString(),
                ActionType = ActivityActionType.Deleted,
                OldValues = oldData,
                NewValues = null,
                PerformedByUserId = userId,
                PerformedByUserName = userName,
                SystemComment = "حذف حضانة مع الفروع",
                UserComment = userComment
            };

            await _activityLogService.CreateAsync(logDto);

            return true;
        }

        //public async Task<bool> SoftDeleteKgAsync(int id)
        //{
        //    var kg = await _kgRepository.GetByIdAsync(id);
        //    if (kg == null) return false;

        //    await _kgRepository.SoftDeleteAsync(id);
        //    return true;
        //}

        public async Task<bool> SoftDeleteKgWithBranchesAsync(int id, string userId, string userName, string? userComment)
        {
            var kg = await _db.Kindergartens
                .Include(k => k.Branches)
                .FirstOrDefaultAsync(k => k.Id == id);

            if (kg == null)
                return false;

            // ✨ نجهز Old Values قبل التعديل
            var oldKgDto = _mapper.Map<KindergartenFullDTO>(kg);
            var oldData = System.Text.Json.JsonSerializer.Serialize(oldKgDto);

            // تنفيذ Soft Delete
            kg.IsDeleted = true;
            kg.UpdatedBy = userId;
            kg.UpdatedOn = DateTime.UtcNow;

            foreach (var branch in kg.Branches.Where(b => b.IsDeleted == false))
            {
                branch.IsDeleted = true;
                branch.UpdatedBy = userId;
                branch.UpdatedOn = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            // توليد NewValues بعد التغييرات
            var newKgDto = _mapper.Map<KindergartenFullDTO>(kg);
            var newData = System.Text.Json.JsonSerializer.Serialize(newKgDto);

            // ✨ إنشاء Log
            var entityType = _db.Model.FindEntityType(typeof(KG));
            var tableName = entityType?.GetTableName() ?? nameof(KG);

            var logDto = new ActivityLogCreateDTO
            {
                EntityName = tableName,
                EntityId = kg.Id.ToString(),
                ActionType = ActivityActionType.SoftDeleted, // أو SoftDeleted لو عندك ActionType مخصوص
                OldValues = oldData,
                NewValues = newData,
                PerformedByUserId = userId,
                PerformedByUserName = userName,
                SystemComment = "Soft delete لحضانة والفروع المرتبطة بها",
                UserComment = userComment
            };

            await _activityLogService.CreateAsync(logDto);

            return true;
        }

        public async Task<List<ActivityLogViewDTO>> GetKgHistoryByKgIdAsync(int kgId)
        {
            var entityType = _db.Model.FindEntityType(typeof(KG));
            var tableName = entityType?.GetTableName() ?? nameof(KG);

            var entityId = kgId.ToString();

            var logs = await _activityLogService.GetEntityHistoryAsync(tableName, entityId);

            return logs;
        }


        #endregion

        #region Functions
        public async Task<string> GenerateKgCodeAsync()
        {
            var lastCode = await _db.Kindergartens
                .OrderByDescending(b => b.KGCode)
                .Select(b => b.KGCode)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (!string.IsNullOrEmpty(lastCode) && lastCode.Length == 8 && lastCode.StartsWith("KG"))
            {
                var numericPart = lastCode.Substring(2);
                if (int.TryParse(numericPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            string newCode;
            bool exists;

            do
            {
                newCode = $"KG{nextNumber.ToString("D6")}";
                exists = await _db.Kindergartens.AnyAsync(b => b.KGCode == newCode);
                nextNumber++;
            }
            while (exists);

            return newCode;
        }
        #endregion
    }

    public interface IKindergartenService
    {
        Task<PagedResult<KindergartenDTO>> GetAllKgsAsync(PaginationFilter filter);
        Task<KindergartenDTO> GetKgByIdAsync(int id);
        Task<KindergartenDTO> CreateKgAsync(KindergartenCreateDTO kgDto, string userId, string userName);
        Task<KindergartenDTO> UpdateKgAsync(KindergartenUpdateDTO dto, string userId, string userName , string? userComment);
        Task<bool> DeleteKgAsync(int id, string userId, string userName, string? userComment);
        //Task<bool> SoftDeleteKgAsync(int id);
        Task<bool> SoftDeleteKgWithBranchesAsync(int id, string userId, string userName, string? userComment);
        Task<string> GenerateKgCodeAsync();
        Task<List<ActivityLogViewDTO>> GetKgHistoryByKgIdAsync(int kgId);
    }
}

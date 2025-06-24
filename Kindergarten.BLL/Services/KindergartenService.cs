using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Kindergarten.BLL.Extensions;
using Kindergarten.BLL.Models;
using Kindergarten.BLL.Models.BranchDTO;
using Kindergarten.BLL.Models.KindergartenDTO;
using Kindergarten.BLL.Repository;
using Kindergarten.DAL.Database;
using Kindergarten.DAL.Entity;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.BLL.Services
{
    public class KindergartenService : IKindergartenService
    {
        #region Prop
        private readonly IGenericRepository<KG, int> _kgRepository;
        private readonly IMapper _mapper;
        public ApplicationContext _db { get; }
        #endregion

        #region CTOR
        public KindergartenService(ApplicationContext db,
                                    IGenericRepository<KG, int> kgRepository,
                                    IMapper mapper)
        {
            _db = db;
            _kgRepository = kgRepository;
            _mapper = mapper;
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

        public async Task<KindergartenDTO> CreateKgAsync(KindergartenCreateDTO dto, string createdBy)
        {
            var kg = _mapper.Map<KG>(dto);
            kg.KGCode = await GenerateKgCodeAsync();
            kg.CreatedBy = createdBy;
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
                branch.CreatedBy = createdBy;

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
                return _mapper.Map<KindergartenDTO>(result);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Branches_BranchCode") == true)
            {
                throw new Exception("An error occurred: A branch code already exists. Please try again.");
            }
        }

        public async Task<KindergartenDTO> UpdateKgAsync(KindergartenUpdateDTO dto, string updatedBy)
        {
            // جلب الحضانة الأصلية مع الفروع متتبعة (Tracking مهم للتحديث)
            var existingKg = await _db.Kindergartens
                .Include(k => k.Branches)
                .FirstOrDefaultAsync(k => k.Id == dto.Id);

            if (existingKg == null)
                return null;

            // تحديث الحقول الأساسية للحضانة
            existingKg.NameAr = dto.NameAr;
            existingKg.NameEn = dto.NameEn;
            existingKg.Address = dto.Address;
            // إذا في خصائص أخرى تحتاج تحديث اضفها هنا
            existingKg.UpdatedBy = updatedBy;
            existingKg.UpdatedOn = DateTime.UtcNow;

            dto.Branches ??= new List<BranchUpdateDTO>();

            // قائمة معرفات الفروع التي وصلت من DTO (للفروع التي موجودة مسبقًا)
            var incomingBranchIds = dto.Branches.Where(b => b.Id > 0).Select(b => b.Id).ToList();

            // عمل Soft Delete للفروع التي موجودة في الحضانة لكن غير موجودة في الطلب الحالي
            foreach (var branch in existingKg.Branches.Where(b => !incomingBranchIds.Contains(b.Id) && b.IsDeleted == false))
            {
                branch.IsDeleted = true;
                branch.UpdatedBy = updatedBy;
                branch.UpdatedOn = DateTime.UtcNow;
            }

            // إعداد HashSet لتتبع BranchCodes المستخدمة فعليًا
            var usedCodes = new HashSet<string>(existingKg.Branches.Select(b => b.BranchCode));

            // دالة مساعدة لتحديد الرقم التالي للكود الجديد
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

            // معالجة الفروع الواردة في DTO
            foreach (var branchDto in dto.Branches)
            {
                if (branchDto.Id > 0)
                {
                    // تحديث فرع موجود
                    var existingBranch = existingKg.Branches.FirstOrDefault(b => b.Id == branchDto.Id);
                    if (existingBranch != null)
                    {
                        existingBranch.NameAr = branchDto.NameAr;
                        existingBranch.NameEn = branchDto.NameEn;
                        existingBranch.Address = branchDto.Address;
                        existingBranch.Phone = branchDto.Phone;
                        existingBranch.Email = branchDto.Email;
                        // لا تغير BranchCode لأنه مفتاح فريد
                        existingBranch.UpdatedBy = updatedBy;
                        existingBranch.UpdatedOn = DateTime.UtcNow;
                        existingBranch.IsDeleted = false; // لو كان محذوف ممكن تعيد تفعيله حسب الحاجة
                    }
                }
                else
                {
                    // إضافة فرع جديد مع توليد BranchCode فريد
                    var newBranch = new Branch
                    {
                        NameAr = branchDto.NameAr,
                        NameEn = branchDto.NameEn,
                        Address = branchDto.Address,
                        Phone = branchDto.Phone,
                        Email = branchDto.Email,
                        CreatedBy = updatedBy,
                        CreatedOn = DateTime.UtcNow,
                        IsDeleted = false,
                        KindergartenId = existingKg.Id
                    };

                    // توليد كود جديد فريد للفرع
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

            // تأكيد وجود فرع واحد نشط على الأقل
            if (!existingKg.Branches.Any(b => b.IsDeleted == false))
                throw new InvalidOperationException("Each kindergarten must have at least one active branch.");

            // حفظ التغييرات في قاعدة البيانات
            await _db.SaveChangesAsync();

            // إرجاع البيانات المحدثة بشكل DTO
            return _mapper.Map<KindergartenDTO>(existingKg);
        }

        public async Task<bool> DeleteKgAsync(int id)
        {
            var kg = await _kgRepository.GetByIdAsync(id);
            if (kg == null) return false;

            await _kgRepository.DeleteAsync(id);
            return true;
        }

        public async Task<bool> SoftDeleteKgAsync(int id)
        {
            var kg = await _kgRepository.GetByIdAsync(id);
            if (kg == null) return false;

            await _kgRepository.SoftDeleteAsync(id);
            return true;
        }

        public async Task<bool> SoftDeleteKgWithBranchesAsync(int id, string updatedBy)
        {
            var kg = await _db.Kindergartens
                .Include(k => k.Branches)
                .FirstOrDefaultAsync(k => k.Id == id);

            if (kg == null) return false;

            kg.IsDeleted = true;
            kg.UpdatedBy = updatedBy;
            kg.UpdatedOn = DateTime.UtcNow;

            foreach (var branch in kg.Branches.Where(b => b.IsDeleted == false))
            {
                branch.IsDeleted = true;
                branch.UpdatedBy = updatedBy;
                branch.UpdatedOn = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return true;
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
        Task<KindergartenDTO> CreateKgAsync(KindergartenCreateDTO kgDto, string createdBy);
        Task<KindergartenDTO> UpdateKgAsync(KindergartenUpdateDTO dto, string updatedBy);
        Task<bool> DeleteKgAsync(int id);
        Task<bool> SoftDeleteKgAsync(int id);
        Task<bool> SoftDeleteKgWithBranchesAsync(int id, string updatedBy);
        Task<string> GenerateKgCodeAsync();
    }
}

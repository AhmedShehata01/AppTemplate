using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Kindergarten.BLL.Models.KGBranchDTO;
using Kindergarten.DAL.Database;
using Kindergarten.DAL.Entity;

namespace Kindergarten.BLL.Services
{
    public class KGBranchService : IKGBranchService
    {
        #region Prop
        private readonly IKindergartenService _kgService;
        private readonly IBranchService _branchService;
        private readonly ApplicationContext _db;
        #endregion

        #region CTOR
        public KGBranchService(
            IKindergartenService kgService,
            IBranchService branchService,
            ApplicationContext db)
        {
            _kgService = kgService;
            _branchService = branchService;
            _db = db;
        }
        public async Task<List<KGBranchDTO>> GetAllKgWithBranchesAsync()
        {
            var allKgs = await _kgService.GetAllKgsWithBranchesAsync(); // بدون Include للفروع
            var result = new List<KGBranchDTO>();

            foreach (var kg in allKgs)
            {
                var branches = await _branchService.GetBranchesByKindergartenIdAsync(kg.Id);
                result.Add(new KGBranchDTO
                {
                    Kg = kg,
                    Branches = branches
                });
            }

            return result;
        }



        public async Task<KGBranchDTO> CreateKgWithBranchesAsync(KGBranchCreateDTO kgBranchCreateDto, string createdBy)
        {
            if (kgBranchCreateDto == null)
                throw new ArgumentNullException(nameof(kgBranchCreateDto));

            // 1. إنشاء الحضانة
            var createdKgDto = await _kgService.CreateKgAsync(kgBranchCreateDto.Kg, createdBy);

            // 2. إنشاء الفروع المرتبطة بالحضانة
            if (kgBranchCreateDto.Branches != null && kgBranchCreateDto.Branches.Any())
            {
                foreach (var branchCreateDto in kgBranchCreateDto.Branches)
                {
                    branchCreateDto.KindergartenId = createdKgDto.Id;
                    await _branchService.CreateBranchAsync(branchCreateDto, createdBy);
                }
            }

            // 3. جلب الفروع من الخدمة المخصصة
            var kg = await _kgService.GetKgByIdAsync(createdKgDto.Id);
            var branches = await _branchService.GetBranchesByKindergartenIdAsync(createdKgDto.Id);

            return new KGBranchDTO
            {
                Kg = kg,
                Branches = branches
            };
        }

        public async Task<KGBranchDTO> GetKgWithBranchesByIdAsync(int kgId)
        {
            // 1. الحصول على الحضانة
            var kg = await _kgService.GetKgByIdAsync(kgId);
            if (kg == null)
                return null;

            // 2. الحصول على الفروع المرتبطة بها
            var branches = await _branchService.GetBranchesByKindergartenIdAsync(kgId);

            // 3. إنشاء كائن KGBranchDTO وارجاعه
            return new KGBranchDTO
            {
                Kg = kg,
                Branches = branches
            };
        }

        public async Task<KGBranchDTO> UpdateKgWithBranchesAsync(KGBranchUpdateDTO kgBranchUpdateDto, string createdBy)
        {
            if (kgBranchUpdateDto == null)
                throw new ArgumentNullException(nameof(kgBranchUpdateDto));

            // 1. Update kindergarten data
            var updatedKgDto = await _kgService.UpdateKgAsync(kgBranchUpdateDto.Kg);

            // 2. Fetch existing branches
            var existingBranches = await _branchService.GetBranchesByKindergartenIdAsync(updatedKgDto.Id);

            // 3. Delete removed branches
            var incomingIds = kgBranchUpdateDto.Branches?
                .Where(b => b.Id > 0)
                .Select(b => b.Id)
                .ToList() ?? new List<int>();

            var toDelete = existingBranches
                .Where(b => !incomingIds.Contains(b.Id))
                .ToList();

            foreach (var b in toDelete)
                await _branchService.DeleteBranchAsync(b.Id);

            // 4. Create or update branches using the unified method
            if (kgBranchUpdateDto.Branches != null)
            {
                foreach (var branchDto in kgBranchUpdateDto.Branches)
                {
                    branchDto.KindergartenId = updatedKgDto.Id;
                    await _branchService.CreateOrUpdateBranchAsync(branchDto, createdBy);
                }
            }

            // 5. Retrieve updated data
            var kg = await _kgService.GetKgByIdAsync(updatedKgDto.Id);
            var branches = await _branchService.GetBranchesByKindergartenIdAsync(updatedKgDto.Id);

            return new KGBranchDTO
            {
                Kg = kg,
                Branches = branches
            };
        }

        public async Task<bool> DeleteKgWithBranchesAsync(int kgId)
        {
            // 1. جلب الفروع المرتبطة بالحضانة
            var branches = await _branchService.GetBranchesByKindergartenIdAsync(kgId);

            // 2. حذف كل فرع (يمكن استخدام soft delete حسب منطقك)
            foreach (var branch in branches)
            {
                var result = await _branchService.DeleteBranchAsync(branch.Id);
                if (!result)
                    return false; // إيقاف التنفيذ لو حذف أي فرع فشل
            }

            // 3. حذف الحضانة نفسها
            var deleteKgResult = await _kgService.DeleteKgAsync(kgId);
            return deleteKgResult;
        }

        public async Task<bool> SoftDeleteKgWithBranchesAsync(int kgId)
        {
            // 1. جلب الفروع المرتبطة بالحضانة
            var branches = await _branchService.GetBranchesByKindergartenIdAsync(kgId);

            // 2. تنفيذ Soft Delete لكل فرع
            foreach (var branch in branches)
            {
                var result = await _branchService.SoftDeleteBranchAsync(branch.Id);
                if (!result)
                    return false; // إيقاف التنفيذ لو فشل حذف ناعم لأي فرع
            }

            // 3. تنفيذ Soft Delete على الحضانة نفسها
            var deleteKgResult = await _kgService.SoftDeleteKgAsync(kgId);
            return deleteKgResult;
        }
        #endregion


    }

    public interface IKGBranchService
    {
        Task<List<KGBranchDTO>> GetAllKgWithBranchesAsync();
        Task<KGBranchDTO> CreateKgWithBranchesAsync(KGBranchCreateDTO kgBranchCreateDto, string createdBy);
        Task<KGBranchDTO> GetKgWithBranchesByIdAsync(int kgId);
        Task<KGBranchDTO> UpdateKgWithBranchesAsync(KGBranchUpdateDTO kgBranchUpdateDto, string createdBy);
        Task<bool> DeleteKgWithBranchesAsync(int kgId);
        Task<bool> SoftDeleteKgWithBranchesAsync(int kgId);

    }
}

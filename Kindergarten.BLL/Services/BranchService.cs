using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Kindergarten.BLL.Models.BranchDTO;
using Kindergarten.BLL.Repository;
using Kindergarten.DAL.Database;
using Kindergarten.DAL.Entity;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.BLL.Services
{
    public class BranchService : IBranchService
    {
        #region Prop
        private readonly IGenericRepository<Branch, int> _branchRepository;
        private readonly IMapper _mapper;
        public ApplicationContext _db { get; }
        #endregion

        #region CTOR
        public BranchService(ApplicationContext db, IGenericRepository<Branch, int> branchRepository, IMapper mapper)
        {
            _db = db;
            _branchRepository = branchRepository;
            _mapper = mapper;
        }
        #endregion

        #region Actions
        public async Task<IEnumerable<BranchDTO>> GetAllBranchesAsync()
        {
            var branches = await _branchRepository.GetAsync();
            return _mapper.Map<IEnumerable<BranchDTO>>(branches);
        }

        public async Task<BranchDTO> GetBranchByIdAsync(int id)
        {
            var branch = await _branchRepository.GetByIdAsync(id);
            return _mapper.Map<BranchDTO>(branch);
        }

        public async Task<BranchDTO> CreateBranchAsync(BranchCreateDTO branchCreateDto, string createdBy)
        {
            var branchEntity = _mapper.Map<Branch>(branchCreateDto);
            branchEntity.CreatedBy = createdBy;

            // Generate unique BranchCode
            branchEntity.BranchCode = await GenerateBranchCodeAsync();

            var createdEntity = await _branchRepository.AddAsync(branchEntity);

            // خذ الـ Entity المحفوظ وحوّله لـ BranchDTO (الذي يحتوي عادةً Id وغيره)
            return _mapper.Map<BranchDTO>(createdEntity);
        }




        public async Task<BranchDTO> UpdateBranchAsync(BranchUpdateDTO branchUpdateDto)
        {
            var existingBranch = await _branchRepository.GetByIdAsync(branchUpdateDto.Id);
            if (existingBranch == null)
                return null;

            // خريطة بيانات التحديث إلى الكيان
            _mapper.Map(branchUpdateDto, existingBranch);

            // احفظ التغييرات (UpdateAsync لا يُعيد شيئًا)
            await _branchRepository.UpdateAsync(existingBranch);

            // استخدم existingBranch بعد الحفظ
            return _mapper.Map<BranchDTO>(existingBranch);
        }



        public async Task<bool> DeleteBranchAsync(int id)
        {
            var existingBranch = await _branchRepository.GetByIdAsync(id);
            if (existingBranch == null)
                return false;

            await _branchRepository.DeleteAsync(id);
            return true;
        }

        public async Task<bool> SoftDeleteBranchAsync(int id)
        {
            var existingBranch = await _branchRepository.GetByIdAsync(id);
            if (existingBranch == null)
                return false;

            await _branchRepository.SoftDeleteAsync(id);
            return true;
        }

        public async Task<List<BranchDTO>> GetBranchesByKindergartenIdAsync(int kindergartenId)
        {
            var branches = await _db.Branches
                .Where(b => b.KindergartenId == kindergartenId)
                .ToListAsync();

            return _mapper.Map<List<BranchDTO>>(branches);
        }


        // New unified method implementation
        public async Task<BranchDTO> CreateOrUpdateBranchAsync(BranchUpdateDTO branchDto, string createdBy)
        {
            if (branchDto == null)
                throw new ArgumentNullException(nameof(branchDto));

            // Create new branch
            if (branchDto.Id == 0)
            {
                // Map to Branch entity
                var entity = _mapper.Map<Branch>(branchDto);
                entity.CreatedBy = createdBy;
                entity.BranchCode = await GenerateBranchCodeAsync();

                var created = await _branchRepository.AddAsync(entity);
                return _mapper.Map<BranchDTO>(created);
            }

            // Update existing branch
            var existing = await _branchRepository.GetByIdAsync(branchDto.Id);
            if (existing == null)
                return null;

            _mapper.Map(branchDto, existing);
            await _branchRepository.UpdateAsync(existing);
            return _mapper.Map<BranchDTO>(existing);
        }
        #endregion

        #region Functions
        public async Task<string> GenerateBranchCodeAsync()
        {
            // Get the last code directly from DB in descending order
            var lastCode = await _db.Branches
                .OrderByDescending(b => b.BranchCode)
                .Select(b => b.BranchCode)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (!string.IsNullOrEmpty(lastCode) && lastCode.Length == 8 && lastCode.StartsWith("BR"))
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
                newCode = $"BR{nextNumber.ToString("D6")}";
                exists = await _db.Branches.AnyAsync(b => b.BranchCode == newCode);
                nextNumber++;
            }
            while (exists);

            return newCode;
        }


        #endregion

    }

    public interface IBranchService
    {
        Task<IEnumerable<BranchDTO>> GetAllBranchesAsync();
        Task<BranchDTO> GetBranchByIdAsync(int id);
        Task<BranchDTO> CreateBranchAsync(BranchCreateDTO branchCreateDto, string createdBy);
        Task<BranchDTO> UpdateBranchAsync(BranchUpdateDTO branchUpdateDto);
        Task<bool> DeleteBranchAsync(int id);
        Task<bool> SoftDeleteBranchAsync(int id);
        Task<string> GenerateBranchCodeAsync();
        Task<List<BranchDTO>> GetBranchesByKindergartenIdAsync(int kindergartenId);

        // New unified method
        Task<BranchDTO> CreateOrUpdateBranchAsync(BranchUpdateDTO branchDto, string createdBy);
    }
}

using System.Linq.Expressions;
using AutoMapper;
using Kindergarten.BLL.Extensions;
using Kindergarten.BLL.Models;
using Kindergarten.BLL.Models.BranchDTO;
using Kindergarten.DAL.Database;
using Kindergarten.DAL.Entity;
using Kindergarten.DAL.Repository;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.BLL.Services
{
    public class BranchService : IBranchService
    {
        #region Props
        private readonly IGenericRepository<Branch, int> _branchRepository;
        private readonly IMapper _mapper;
        private readonly ApplicationContext _db;
        #endregion

        #region CTOR
        public BranchService(IGenericRepository<Branch, int> branchRepository, IMapper mapper, ApplicationContext db)
        {
            _branchRepository = branchRepository;
            _mapper = mapper;
            _db = db;
        }
        #endregion

        #region Actions
        public async Task<PagedResult<BranchDTO>> GetAllBranchesAsync(PaginationFilter filter)
        {
            Expression<Func<Branch, bool>> where = b =>
                b.IsDeleted == false &&
                (string.IsNullOrEmpty(filter.SearchText) || b.NameEn.Contains(filter.SearchText));

            Func<IQueryable<Branch>, IOrderedQueryable<Branch>>? orderBy = query =>
                query.ApplySorting(filter.SortBy, filter.SortDirection);

            var result = await _branchRepository.GetAsync(
                filter: where,
                page: filter.Page,
                pageSize: filter.PageSize,
                orderBy: orderBy,
                noTrack: true
            );

            var totalCount = await _branchRepository.CountAsync(where);

            return new PagedResult<BranchDTO>
            {
                Data = _mapper.Map<List<BranchDTO>>(result),
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<BranchDTO?> GetBranchByIdAsync(int id)
        {
            var branch = await _branchRepository.GetByIdAsync(id);
            return branch == null ? null : _mapper.Map<BranchDTO>(branch);
        }

        public async Task<BranchDTO> CreateBranchAsync(BranchCreateDTO dto, string createdBy)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var branch = _mapper.Map<Branch>(dto);
            branch.CreatedBy = createdBy;
            branch.CreatedOn = DateTime.UtcNow;
            branch.BranchCode = await GenerateBranchCodeAsync();

            var result = await _branchRepository.AddAsync(branch);
            return _mapper.Map<BranchDTO>(result);
        }

        public async Task<BranchDTO?> UpdateBranchAsync(BranchUpdateDTO dto, string updatedBy)
        {
            var existingBranch = await _db.Branches.FirstOrDefaultAsync(b => b.Id == dto.Id);
            if (existingBranch == null)
                return null;

            _mapper.Map(dto, existingBranch);
            existingBranch.UpdatedBy = updatedBy;
            existingBranch.UpdatedOn = DateTime.UtcNow;

            await _branchRepository.UpdateAsync(existingBranch);
            return _mapper.Map<BranchDTO>(existingBranch);
        }

        public async Task<bool> DeleteBranchAsync(int id)
        {
            var branch = await _branchRepository.GetByIdAsync(id);
            if (branch == null) return false;

            await _branchRepository.DeleteAsync(id);
            return true;
        }

        public async Task<bool> SoftDeleteBranchAsync(int id)
        {
            var branch = await _branchRepository.GetByIdAsync(id);
            if (branch == null) return false;

            await _branchRepository.SoftDeleteAsync(id);
            return true;
        }

        public async Task<List<BranchDTO>> GetBranchesByKindergartenIdAsync(int kindergartenId)
        {
            var branches = await _branchRepository.GetAsync(
                filter: b => b.KindergartenId == kindergartenId,
                noTrack: true
            );
            return _mapper.Map<List<BranchDTO>>(branches);
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
        Task<PagedResult<BranchDTO>> GetAllBranchesAsync(PaginationFilter filter);
        Task<BranchDTO?> GetBranchByIdAsync(int id);
        Task<BranchDTO> CreateBranchAsync(BranchCreateDTO dto, string createdBy);
        Task<BranchDTO?> UpdateBranchAsync(BranchUpdateDTO dto, string createdBy);
        Task<bool> DeleteBranchAsync(int id);
        Task<bool> SoftDeleteBranchAsync(int id);
        Task<string> GenerateBranchCodeAsync();
        Task<List<BranchDTO>> GetBranchesByKindergartenIdAsync(int kindergartenId);
    }
}

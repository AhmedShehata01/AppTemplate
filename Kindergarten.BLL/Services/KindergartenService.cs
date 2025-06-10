using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
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
        public KindergartenService(ApplicationContext db, IGenericRepository<KG, int> kgRepository, IMapper mapper)
        {
            _db = db;
            _kgRepository = kgRepository;
            _mapper = mapper;
        }
        #endregion

        #region Actions
        public async Task<IEnumerable<KindergartenDTO>> GetAllKgsWithBranchesAsync()
        {
            var kgsWithBranches = await _db.Kindergartens
                .Include(k => k.Branches)
                .Where(k => k.IsDeleted == false)
                .ToListAsync();

            return _mapper.Map<IEnumerable<KindergartenDTO>>(kgsWithBranches);
        }

        public async Task<KindergartenDTO> GetKgByIdAsync(int id)
        {
            var kg = await _kgRepository.GetByIdAsync(id);
            return _mapper.Map<KindergartenDTO>(kg);
        }

        // ← هذا التابع تم تعديله:
        public async Task<KindergartenDTO> CreateKgAsync(KindergartenCreateDTO createDto, string createdBy)
        {
            var entity = _mapper.Map<KG>(createDto);
            entity.CreatedBy = createdBy;
            entity.KGCode = await GenerateKgCodeAsync();

            var createdEntity = await _kgRepository.AddAsync(entity);
            // الآن نعيد KindergartenDTO بدل CreateDTO
            return _mapper.Map<KindergartenDTO>(createdEntity);
        }

        // ← هذا التابع تم تعديله:
        public async Task<KindergartenDTO> UpdateKgAsync(KindergartenUpdateDTO updateDto)
        {
            var existing = await _kgRepository.GetByIdAsync(updateDto.Id);
            if (existing == null)
                return null;

            _mapper.Map(updateDto, existing);
            // نفترض UpdateAsync لا تُعيد قيمة، في هذه الحالة:
            await _kgRepository.UpdateAsync(existing);

            // نعيد الكائن الموجود بعد التحديث
            return _mapper.Map<KindergartenDTO>(existing);
        }

        public async Task<bool> DeleteKgAsync(int id)
        {
            var existingKg = await _kgRepository.GetByIdAsync(id);
            if (existingKg == null)
                return false;

            await _kgRepository.DeleteAsync(id);
            return true;
        }

        public async Task<bool> SoftDeleteKgAsync(int id)
        {
            var existingKg = await _kgRepository.GetByIdAsync(id);
            if (existingKg == null)
                return false;

            await _kgRepository.SoftDeleteAsync(id);
            return true;
        }
        #endregion

        #region Functions
        public async Task<string> GenerateKgCodeAsync()
        {
            // Get the last code directly from DB in descending order
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
        Task<IEnumerable<KindergartenDTO>> GetAllKgsWithBranchesAsync();
        Task<KindergartenDTO> GetKgByIdAsync(int id);
        // عدلنا نوع الإرجاع هنا إلى KindergartenDTO
        Task<KindergartenDTO> CreateKgAsync(KindergartenCreateDTO kgDto, string createdBy);
        // وعدلنا نوع الإرجاع هنا أيضاً
        Task<KindergartenDTO> UpdateKgAsync(KindergartenUpdateDTO kgDto);
        Task<bool> DeleteKgAsync(int id);
        Task<bool> SoftDeleteKgAsync(int id);
        Task<string> GenerateKgCodeAsync();
    }
}
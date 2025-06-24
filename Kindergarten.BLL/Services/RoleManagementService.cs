using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Kindergarten.BLL.Extensions;
using Kindergarten.BLL.Models;
using Kindergarten.BLL.Models.RoleManagementDTO;
using Kindergarten.DAL.Database;
using Kindergarten.DAL.Extend;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.BLL.Services
{
    public class RoleManagementService : IRoleManagementService
    {
        #region Props
        private readonly ApplicationContext _context;
        private readonly IMapper _mapper;
        #endregion

        #region Ctor
        public RoleManagementService(ApplicationContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        #endregion

        #region Actions

        public async Task<PagedResult<ApplicationRoleDTO>> GetAllRolesAsync(PaginationFilter filter)
        {
            var searchText = filter.SearchText?.Trim().ToLower();

            var query = _context.Roles
                .Where(r => !r.IsDeleted &&
                    (string.IsNullOrEmpty(searchText) ||
                     r.Name.ToLower().Contains(searchText)));

            query = query.ApplySorting(filter.SortBy, filter.SortDirection);

            var totalCount = await query.CountAsync();

            var pagedData = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .AsNoTracking()
                .ToListAsync();

            var mapped = _mapper.Map<List<ApplicationRoleDTO>>(pagedData);

            return new PagedResult<ApplicationRoleDTO>
            {
                Data = mapped,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<ApplicationRoleDTO> GetRoleByIdAsync(string roleId)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
            return role == null ? null : _mapper.Map<ApplicationRoleDTO>(role);
        }

        public async Task<string> CreateRoleAsync(CreateRoleDTO dto)
        {
            if (await _context.Roles.AnyAsync(r => r.Name == dto.Name))
                throw new Exception("Role name already exists.");

            var role = _mapper.Map<ApplicationRole>(dto);
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
            return role.Id;
        }

        public async Task<bool> UpdateRoleAsync(UpdateRoleDTO dto)
        {
            var existing = await _context.Roles.FirstOrDefaultAsync(r => r.Id == dto.Id);
            if (existing == null) return false;

            if (await _context.Roles.AnyAsync(r => r.Id != dto.Id && r.Name == dto.Name))
                throw new Exception("Another role already uses the same name.");

            existing.Name = dto.Name;
            existing.IsActive = dto.IsActive;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleRoleStatusAsync(ToggleRoleStatusDTO dto)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == dto.RoleId);
            if (role == null) return false;

            role.IsActive = dto.IsActive;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<RoleWithRoutesDTO>> GetRolesWithRoutesAsync()
        {
            var roles = await _context.Roles
                .Include(r => r.RoleSecuredRoutes)
                    .ThenInclude(rsr => rsr.SecuredRoute)
                .Where(r => !r.IsDeleted)
                .ToListAsync();

            return _mapper.Map<List<RoleWithRoutesDTO>>(roles);
        }

        public async Task<bool> DeleteRoleAsync(string roleId)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
            if (role == null) return false;

            role.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion
    }

    public interface IRoleManagementService
    {
        Task<PagedResult<ApplicationRoleDTO>> GetAllRolesAsync(PaginationFilter filter);
        Task<ApplicationRoleDTO> GetRoleByIdAsync(string roleId);
        Task<string> CreateRoleAsync(CreateRoleDTO dto);
        Task<bool> UpdateRoleAsync(UpdateRoleDTO dto);
        Task<bool> ToggleRoleStatusAsync(ToggleRoleStatusDTO dto);
        Task<List<RoleWithRoutesDTO>> GetRolesWithRoutesAsync();
        Task<bool> DeleteRoleAsync(string roleId);
    }
}

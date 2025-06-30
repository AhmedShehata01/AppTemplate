using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Kindergarten.BLL.Models;
using Kindergarten.BLL.Models.RoleManagementDTO;
using Kindergarten.BLL.Models.UserManagementDTO;
using Kindergarten.DAL.Database;
using Kindergarten.DAL.Extend;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.BLL.Services.IdentityServices
{
    public class RoleManagementService : IRoleManagementService
    {
        #region Prop
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

        #region
        public async Task<PagedResult<ApplicationRoleDTO>> GetAllPaginatedAsync(PaginationFilter filter)
        {
            var query = _context.Roles
                .Where(r => !r.IsDeleted &&
                    (string.IsNullOrEmpty(filter.SearchText) ||
                     r.Name.ToLower().Contains(filter.SearchText.ToLower())));

            // Sorting
            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                var isDesc = filter.SortDirection?.ToLower() == "desc";
                switch (filter.SortBy.ToLower())
                {
                    case "name":
                        query = isDesc ? query.OrderByDescending(r => r.Name) : query.OrderBy(r => r.Name);
                        break;
                    case "createdon":
                        query = isDesc ? query.OrderByDescending(r => r.CreatedOn) : query.OrderBy(r => r.CreatedOn);
                        break;
                    default:
                        query = query.OrderBy(r => r.Name);
                        break;
                }
            }
            else
            {
                query = query.OrderBy(r => r.Name);
            }

            var totalCount = await query.CountAsync();

            var data = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var mappedData = _mapper.Map<List<ApplicationRoleDTO>>(data);

            return new PagedResult<ApplicationRoleDTO>
            {
                Data = mappedData,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<ApplicationRoleDTO> GetByIdAsync(string roleId)
        {
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.Id == roleId && !r.IsDeleted);

            return role == null ? null : _mapper.Map<ApplicationRoleDTO>(role);
        }

        public async Task<bool> CreateRoleAsync(CreateRoleDTO dto)
        {
            var normalizedName = dto.Name.ToUpper();

            var existing = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name.ToUpper() == normalizedName);

            if (existing != null)
            {
                if (existing.IsDeleted)
                    throw new Exception("Another role already uses the same name and is marked as deleted.");
                if (!existing.IsActive)
                    throw new Exception("Another role already uses the same name and is marked as inactive.");

                throw new Exception("Another role already uses the same name.");
            }

            var newRole = _mapper.Map<ApplicationRole>(dto);
            _context.Roles.Add(newRole);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateRoleAsync(UpdateRoleDTO dto)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == dto.Id);
            if (role == null || role.IsDeleted)
                return false;

            var normalizedName = dto.Name.ToUpper();

            var existing = await _context.Roles
                .FirstOrDefaultAsync(r => r.Id != dto.Id && r.Name.ToUpper() == normalizedName);

            if (existing != null)
            {
                if (existing.IsDeleted)
                    throw new Exception("Another role already uses the same name and is marked as deleted.");
                if (!existing.IsActive)
                    throw new Exception("Another role already uses the same name and is marked as inactive.");

                throw new Exception("Another role already uses the same name.");
            }

            // 🛑 Prevent deactivation if currently active and new value is false
            if (role.IsActive && dto.IsActive == false)
            {
                // 1. Check if assigned to users
                var isAssignedToUsers = await _context.UserRoles
                    .AnyAsync(ur => ur.RoleId == dto.Id);
                if (isAssignedToUsers)
                    throw new Exception("Cannot deactivate role while it is assigned to users.");

                // 2. Check if assigned to secured routes
                var isAssignedToRoutes = await _context.RoleSecuredRoutes
                    .AnyAsync(rsr => rsr.RoleId == dto.Id);
                if (isAssignedToRoutes)
                    throw new Exception("Cannot deactivate role while it is assigned to secured routes.");
            }

            role.Name = dto.Name;
            role.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<bool> ToggleRoleStatusAsync(string roleId)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId && !r.IsDeleted);
            if (role == null)
                return false;

            // 🔒 Only check constraints if trying to deactivate
            if (role.IsActive)
            {
                // ❌ Check if assigned to any users
                var hasUsers = await _context.UserRoles.AnyAsync(ur => ur.RoleId == roleId);
                if (hasUsers)
                    throw new Exception("Cannot deactivate role while it is assigned to users.");

                // ❌ Check if assigned to any secured routes
                var hasSecuredRoutes = await _context.RoleSecuredRoutes.AnyAsync(rsr => rsr.RoleId == roleId);
                if (hasSecuredRoutes)
                    throw new Exception("Cannot deactivate role while it is assigned to secured routes.");
            }

            // ✅ Toggle status
            role.IsActive = !role.IsActive;
            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<bool> DeleteRoleAsync(string roleId)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId && !r.IsDeleted);
            if (role == null)
                return false;

            // ❌ Prevent deletion if role is assigned to any users
            var hasUsers = await _context.UserRoles.AnyAsync(ur => ur.RoleId == roleId);
            if (hasUsers)
                throw new Exception("Cannot delete role while it is assigned to users.");

            // ❌ Prevent deletion if role is assigned to any secured routes
            var hasSecuredRoutes = await _context.RoleSecuredRoutes.AnyAsync(rsr => rsr.RoleId == roleId);
            if (hasSecuredRoutes)
                throw new Exception("Cannot delete role while it is assigned to secured routes.");

            // ✅ Soft delete
            role.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<List<RoleWithRoutesDTO>> GetRolesWithRoutesAsync()
        {
            var roles = await _context.Roles
                .Include(r => r.RoleSecuredRoutes)
                    .ThenInclude(rs => rs.SecuredRoute)
                .Where(r => !r.IsDeleted)
                .ToListAsync();

            return _mapper.Map<List<RoleWithRoutesDTO>>(roles);
        }

        public async Task<List<DropdownRoleDTO>> GetDropdownRolesAsync()
        {
            var roles = await _context.Roles
                .Where(r => !r.IsDeleted && r.IsActive)
                .ToListAsync();

            return _mapper.Map<List<DropdownRoleDTO>>(roles);
        }

        public async Task<List<ApplicationUserDTO>> GetUsersByRoleAsync(string roleId)
        {
            var userIds = await _context.UserRoles
                .Where(ur => ur.RoleId == roleId)
                .Select(ur => ur.UserId)
                .ToListAsync();

            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id) && !u.IsDeleted)
                .ToListAsync();

            return _mapper.Map<List<ApplicationUserDTO>>(users);
        }

        public async Task<bool> RemoveUserRoleAsync(string userId, string roleId)
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (userRole == null) return false;

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion
    }


    public interface IRoleManagementService
    {
        Task<PagedResult<ApplicationRoleDTO>> GetAllPaginatedAsync(PaginationFilter filter);
        Task<ApplicationRoleDTO> GetByIdAsync(string roleId);                          // Get role by ID
        Task<bool> CreateRoleAsync(CreateRoleDTO dto);                      // Create a new role
        Task<bool> UpdateRoleAsync(UpdateRoleDTO dto);                      // Update existing role
        Task<bool> ToggleRoleStatusAsync(string roleId);                    // Toggle active/inactive
        Task<bool> DeleteRoleAsync(string roleId);                          // Delete role
        Task<List<RoleWithRoutesDTO>> GetRolesWithRoutesAsync();            // Get all roles + routes
        Task<List<DropdownRoleDTO>> GetDropdownRolesAsync();

        Task<List<ApplicationUserDTO>> GetUsersByRoleAsync(string roleId);
        Task<bool> RemoveUserRoleAsync(string userId, string roleId);
    }
}

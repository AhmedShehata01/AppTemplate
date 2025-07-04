using System.Text.Json;
using AutoMapper;
using AppTemplate.BLL.Models;
using AppTemplate.BLL.Models.ActivityLogDTO;
using AppTemplate.BLL.Models.RoleManagementDTO;
using AppTemplate.BLL.Models.UserManagementDTO;
using AppTemplate.DAL.Database;
using AppTemplate.DAL.Enum;
using AppTemplate.DAL.Extend;
using Microsoft.EntityFrameworkCore;

namespace AppTemplate.BLL.Services.IdentityServices
{
    public class RoleManagementService : IRoleManagementService
    {
        #region Prop
        private readonly ApplicationContext _context;
        private readonly IMapper _mapper;
        private readonly IActivityLogService _activityLogService;
        #endregion

        #region Ctor
        public RoleManagementService(ApplicationContext context, IMapper mapper, IActivityLogService activityLogService)
        {
            _context = context;
            _mapper = mapper;
            _activityLogService = activityLogService;
        }
        #endregion

        #region

        public async Task<PagedResult<ApplicationRoleDTO>> GetAllPaginatedAsync(PaginationFilter filter)
        {
            var query = _context.Roles
                .Where(r => !r.IsDeleted &&
                    (string.IsNullOrEmpty(filter.SearchText) ||
                     r.Name.ToLower().Contains(filter.SearchText.ToLower())));

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

            if (role == null)
                throw new KeyNotFoundException("Role not found.");

            return _mapper.Map<ApplicationRoleDTO>(role);
        }

        public async Task<string> CreateRoleAsync(CreateRoleDTO dto, string? userId, string? userName)
        {
            var normalizedName = dto.Name.ToUpper();

            var existing = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name.ToUpper() == normalizedName);

            if (existing != null)
            {
                if (existing.IsDeleted)
                    throw new InvalidOperationException("A role with this name already exists and is marked as deleted.");

                if (!existing.IsActive)
                    throw new InvalidOperationException("A role with this name already exists and is inactive.");

                throw new InvalidOperationException("A role with this name already exists.");
            }

            var newRole = _mapper.Map<ApplicationRole>(dto);
            _context.Roles.Add(newRole);
            await _context.SaveChangesAsync();

            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = nameof(ApplicationRole),
                EntityId = newRole.Id,
                ActionType = ActivityActionType.Created,
                SystemComment = $"تم إنشاء رول جديد باسم: {newRole.Name}",
                PerformedByUserId = userId,
                PerformedByUserName = userName,
                NewValues = JsonSerializer.Serialize(_mapper.Map<ApplicationRoleDTO>(newRole))
            });

            return newRole.Id;
        }

        public async Task UpdateRoleAsync(UpdateRoleDTO dto, string? userId, string? userName)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == dto.Id);
            if (role == null || role.IsDeleted)
                throw new KeyNotFoundException("Role not found or has been deleted.");

            var normalizedName = dto.Name.ToUpper();

            var existing = await _context.Roles
                .FirstOrDefaultAsync(r => r.Id != dto.Id && r.Name.ToUpper() == normalizedName);

            if (existing != null)
            {
                if (existing.IsDeleted)
                    throw new InvalidOperationException("Another role already uses the same name and is marked as deleted.");

                if (!existing.IsActive)
                    throw new InvalidOperationException("Another role already uses the same name and is inactive.");

                throw new InvalidOperationException("Another role already uses the same name.");
            }

            if (role.IsActive && dto.IsActive == false)
            {
                var isAssignedToUsers = await _context.UserRoles.AnyAsync(ur => ur.RoleId == dto.Id);
                if (isAssignedToUsers)
                    throw new InvalidOperationException("Cannot deactivate the role while it is assigned to users.");

                var isAssignedToRoutes = await _context.RoleSecuredRoutes.AnyAsync(rsr => rsr.RoleId == dto.Id);
                if (isAssignedToRoutes)
                    throw new InvalidOperationException("Cannot deactivate the role while it is assigned to secured routes.");
            }

            var oldDto = _mapper.Map<ApplicationRoleDTO>(role);
            var oldData = JsonSerializer.Serialize(oldDto);

            role.Name = dto.Name;
            role.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            var newDto = _mapper.Map<ApplicationRoleDTO>(role);
            var newData = JsonSerializer.Serialize(newDto);

            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = nameof(ApplicationRole),
                EntityId = role.Id,
                ActionType = ActivityActionType.Updated,
                SystemComment = $"تم تعديل بيانات الرول: {role.Name}",
                PerformedByUserId = userId,
                PerformedByUserName = userName,
                OldValues = oldData,
                NewValues = newData
            });
        }

        public async Task ToggleRoleStatusAsync(string roleId, string? userId, string? userName)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId && !r.IsDeleted);
            if (role == null)
                throw new KeyNotFoundException("Role not found.");

            if (role.IsActive)
            {
                var hasUsers = await _context.UserRoles.AnyAsync(ur => ur.RoleId == roleId);
                if (hasUsers)
                    throw new InvalidOperationException("Cannot deactivate the role while it is assigned to users.");

                var hasSecuredRoutes = await _context.RoleSecuredRoutes.AnyAsync(rsr => rsr.RoleId == roleId);
                if (hasSecuredRoutes)
                    throw new InvalidOperationException("Cannot deactivate the role while it is assigned to secured routes.");
            }

            var oldDto = _mapper.Map<ApplicationRoleDTO>(role);
            var oldData = JsonSerializer.Serialize(oldDto);

            role.IsActive = !role.IsActive;
            await _context.SaveChangesAsync();

            var newDto = _mapper.Map<ApplicationRoleDTO>(role);
            var newData = JsonSerializer.Serialize(newDto);

            var actionType = role.IsActive ? ActivityActionType.Activated : ActivityActionType.Deactivated;

            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = nameof(ApplicationRole),
                EntityId = role.Id,
                ActionType = actionType,
                SystemComment = $"تم {(role.IsActive ? "تفعيل" : "تعطيل")} الرول: {role.Name}",
                PerformedByUserId = userId,
                PerformedByUserName = userName,
                OldValues = oldData,
                NewValues = newData
            });
        }

        public async Task DeleteRoleAsync(string roleId, string? userId, string? userName)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId && !r.IsDeleted);
            if (role == null)
                throw new KeyNotFoundException("Role not found.");

            var hasUsers = await _context.UserRoles.AnyAsync(ur => ur.RoleId == roleId);
            if (hasUsers)
                throw new InvalidOperationException("Cannot delete the role while it is assigned to users.");

            var hasSecuredRoutes = await _context.RoleSecuredRoutes.AnyAsync(rsr => rsr.RoleId == roleId);
            if (hasSecuredRoutes)
                throw new InvalidOperationException("Cannot delete the role while it is assigned to secured routes.");

            var oldDto = _mapper.Map<ApplicationRoleDTO>(role);
            var oldData = JsonSerializer.Serialize(oldDto);

            role.IsDeleted = true;
            await _context.SaveChangesAsync();

            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = nameof(ApplicationRole),
                EntityId = role.Id,
                ActionType = ActivityActionType.Deleted,
                SystemComment = $"تم حذف الرول: {role.Name}",
                PerformedByUserId = userId,
                PerformedByUserName = userName,
                OldValues = oldData,
                NewValues = null
            });
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
            var roleExists = await _context.Roles.AnyAsync(r => r.Id == roleId && !r.IsDeleted);
            if (!roleExists)
                throw new KeyNotFoundException("Role not found.");

            var userIds = await _context.UserRoles
                .Where(ur => ur.RoleId == roleId)
                .Select(ur => ur.UserId)
                .ToListAsync();

            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id) && !u.IsDeleted)
                .ToListAsync();

            return _mapper.Map<List<ApplicationUserDTO>>(users);
        }


        public async Task RemoveUserRoleAsync(string userId, string roleId, string? performedByUserId, string? performedByUserName)
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (userRole == null)
                throw new KeyNotFoundException("User-role assignment not found.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();

            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = "UserRole",
                EntityId = $"{userId}-{roleId}",
                ActionType = ActivityActionType.DeletedChildEntity,
                SystemComment = $"تم إزالة الرول '{role?.Name}' من المستخدم '{user?.UserName}'.",
                PerformedByUserId = performedByUserId,
                PerformedByUserName = performedByUserName,
                OldValues = JsonSerializer.Serialize(new
                {
                    UserId = userId,
                    UserName = user?.UserName,
                    RoleId = roleId,
                    RoleName = role?.Name
                }),
                NewValues = null
            });
        }


        #endregion
    }


    public interface IRoleManagementService
    {
        Task<PagedResult<ApplicationRoleDTO>> GetAllPaginatedAsync(PaginationFilter filter);
        Task<ApplicationRoleDTO> GetByIdAsync(string roleId);                          // Get role by ID
        Task<string> CreateRoleAsync(CreateRoleDTO dto, string? userId, string? userName);   // Create a new role
        Task UpdateRoleAsync(UpdateRoleDTO dto, string? userId, string? userName);    // Update existing role
        Task ToggleRoleStatusAsync(string roleId, string? userId, string? userName);                    // Toggle active/inactive
        Task DeleteRoleAsync(string roleId, string? userId, string? userName);                          // Delete role
        Task<List<RoleWithRoutesDTO>> GetRolesWithRoutesAsync();            // Get all roles + routes
        Task<List<DropdownRoleDTO>> GetDropdownRolesAsync();
        Task<List<ApplicationUserDTO>> GetUsersByRoleAsync(string roleId);
        Task RemoveUserRoleAsync(string userId, string roleId, string? performedByUserId, string? performedByUserName);
    }
}

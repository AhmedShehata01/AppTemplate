using System.Text.Json;
using AppTemplate.BLL.Models.ActivityLogDTO;
using AppTemplate.DAL.Database;
using AppTemplate.DAL.Enum;
using AppTemplate.DAL.Extend;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AppTemplate.BLL.Services.IdentityServices
{
    public class UserRoleManagementService : IUserRoleManagementService
    {
        #region Prop
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ApplicationContext _context;
        private readonly IActivityLogService _activityLogService;
        #endregion

        #region Ctor
        public UserRoleManagementService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ApplicationContext context,
            IActivityLogService activityLogService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _activityLogService = activityLogService;
        }
        #endregion

        #region Actions

        // جلب كل أسماء الأدوار من قاعدة البيانات (المفعلة وغير المحذوفة)
        public async Task<List<string>> GetAllRolesAsync()
        {
            var roles = _roleManager.Roles
                .Where(r => !r.IsDeleted && r.IsActive)
                .Select(r => r.Name)
                .ToList();

            return await Task.FromResult(roles);
        }

        // جلب أدوار المستخدم بحسب userId
        public async Task<List<string>> GetUserRolesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new InvalidOperationException("User not found.");

            var userRoles = await _userManager.GetRolesAsync(user);

            var validRoles = await _roleManager.Roles
                .Where(r => r.IsActive && !r.IsDeleted && userRoles.Contains(r.Name))
                .Select(r => r.Name)
                .ToListAsync();

            return validRoles;
        }

        // تعيين أدوار للمستخدم: إزالة الأدوار القديمة واستبدالها بالأدوار الجديدة
        public async Task<bool> AssignRolesToUserAsync(
            string userId,
            List<string> requestedRoles,
            string? performedByUserId,
            string? performedByUserName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new InvalidOperationException("User not found.");

            var existingRoles = await _userManager.GetRolesAsync(user);

            var validRolesFromDb = await _roleManager.Roles
                .Where(r => r.IsActive && !r.IsDeleted)
                .Select(r => r.Name)
                .ToListAsync();

            var validRequestedRoles = requestedRoles
                .Where(r => validRolesFromDb.Contains(r))
                .ToList();

            var rolesToAdd = validRequestedRoles
                .Except(existingRoles)
                .ToList();

            var rolesToRemove = existingRoles
                .Except(validRequestedRoles)
                .Where(r => validRolesFromDb.Contains(r))
                .ToList();

            if (rolesToRemove.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to remove roles from user. Details: {errors}");
                }
            }

            // تنظيف أي userRoles للأدوار المحذوفة أو غير النشطة
            var invalidRoleIds = await _roleManager.Roles
                .Where(r => !r.IsActive || r.IsDeleted)
                .Select(r => r.Id)
                .ToListAsync();

            var orphanUserRoles = _context.UserRoles
                .Where(ur => ur.UserId == userId && invalidRoleIds.Contains(ur.RoleId));

            if (orphanUserRoles.Any())
            {
                _context.UserRoles.RemoveRange(orphanUserRoles);
                await _context.SaveChangesAsync();
            }

            if (rolesToAdd.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to add roles to user. Details: {errors}");
                }
            }

            var oldRolesJson = JsonSerializer.Serialize(existingRoles);
            var newRolesJson = JsonSerializer.Serialize(validRequestedRoles);

            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = "UserRoles",
                EntityId = userId,
                ActionType = ActivityActionType.Updated,
                SystemComment = $"تم تحديث أدوار المستخدم {user.UserName}.",
                PerformedByUserId = performedByUserId,
                PerformedByUserName = performedByUserName,
                OldValues = oldRolesJson,
                NewValues = newRolesJson
            });

            return true;
        }

        #endregion
    }
    public interface IUserRoleManagementService
    {
        Task<List<string>> GetAllRolesAsync();                   // جلب كل أسماء الأدوار
        Task<List<string>> GetUserRolesAsync(string userId);     // جلب أدوار مستخدم معين
        Task<bool> AssignRolesToUserAsync(string userId, List<string> roles, string? performedByUserId, string? performedByUserName); // تعيين أدوار للمستخدم
    }
}

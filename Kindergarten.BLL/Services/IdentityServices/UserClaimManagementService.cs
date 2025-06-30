using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Kindergarten.BLL.Models.ActivityLogDTO;
using Kindergarten.BLL.Models.ClaimsDTO;
using Kindergarten.DAL.Enum;
using Kindergarten.DAL.Extend;
using Microsoft.AspNetCore.Identity;

namespace Kindergarten.BLL.Services.IdentityServices
{
    public class UserClaimManagementService : IUserClaimManagementService
    {
        #region Prop
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IActivityLogService _activityLogService;
        #endregion

        #region Ctor
        public UserClaimManagementService(UserManager<ApplicationUser> userManager, IActivityLogService activityLogService)
        {
            _userManager = userManager;
            _activityLogService = activityLogService;
        }
        #endregion

        #region Actions
        public Task<List<ClaimDTO>> GetAllClaimsAsync()
        {
            var allClaims = ClaimsStore.AllClaims
                .Select(c => new ClaimDTO { Type = c.Type, Value = c.Value })
                .ToList();

            return Task.FromResult(allClaims);
        }
        public async Task<List<ClaimDTO>> GetUserClaimsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found.");

            var claims = await _userManager.GetClaimsAsync(user);
            return claims.Select(c => new ClaimDTO { Type = c.Type, Value = c.Value }).ToList();
        }
        public async Task<bool> AssignClaimsToUserAsync(string userId, List<string> claimTypes, string? performedByUserId, string? performedByUserName)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new Exception("User not found.");

            var existingClaims = await _userManager.GetClaimsAsync(user);

            var desiredClaims = claimTypes
                .Distinct()
                .Select(type => new Claim(type, "true"))
                .ToList();

            var claimsToAdd = desiredClaims
                .Where(dc => !existingClaims.Any(ec => ec.Type == dc.Type && ec.Value == dc.Value))
                .ToList();

            var claimsToRemove = existingClaims
                .Where(ec => !desiredClaims.Any(dc => dc.Type == ec.Type && dc.Value == ec.Value))
                .ToList();

            // أولًا: نفذ الحذف والإضافة
            if (claimsToRemove.Any())
            {
                var removeResult = await _userManager.RemoveClaimsAsync(user, claimsToRemove);
                if (!removeResult.Succeeded)
                    throw new Exception($"Failed to remove claims for user {user.UserName}.");
            }

            if (claimsToAdd.Any())
            {
                var addResult = await _userManager.AddClaimsAsync(user, claimsToAdd);
                if (!addResult.Succeeded)
                    throw new Exception($"Failed to add claims for user {user.UserName}.");
            }

            // بعد التعديل نجيب الـ Claims الجديدة من الداتا ستور (لو حابب تكون دقيقة)
            var updatedClaims = await _userManager.GetClaimsAsync(user);

            // سجل الـ ActivityLog مرة واحدة مع OldValues و NewValues
            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = "UserClaims",
                EntityId = userId,
                ActionType = ActivityActionType.Updated,
                SystemComment = $"تم تحديث صلاحيات المستخدم {user.UserName}.",
                PerformedByUserId = performedByUserId,
                PerformedByUserName = performedByUserName,
                OldValues = JsonSerializer.Serialize(existingClaims.Select(c => new { c.Type, c.Value })),
                NewValues = JsonSerializer.Serialize(updatedClaims.Select(c => new { c.Type, c.Value }))
            });

            return true;
        }




        #endregion
    }

    public interface IUserClaimManagementService
    {
        Task<List<ClaimDTO>> GetAllClaimsAsync(); // جلب كل الـ claims المتاحة
        Task<List<ClaimDTO>> GetUserClaimsAsync(string userId); // جلب claims المستخدم
        Task<bool> AssignClaimsToUserAsync(string userId, List<string> claimTypes , string? performedByUserId, string? performedByUserName); // تعيين claims للمستخدم
    }

}

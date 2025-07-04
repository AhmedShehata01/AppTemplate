using System.Security.Claims;
using AppTemplate.BLL.Helper;
using AppTemplate.BLL.Models;
using AppTemplate.BLL.Models.UserProfileDTO;
using AppTemplate.BLL.Services;
using AppTemplate.DAL.Database;
using AppTemplate.DAL.Extend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AppTemplate.API.Controllers
{
    public class PersonalProfilesController : BaseController
    {

        #region Prop
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ApplicationContext db;
        private readonly IUserProfileService _iUserProfileService;
        #endregion


        #region CTOR
        public PersonalProfilesController(UserManager<ApplicationUser> _userManager,
                                        RoleManager<ApplicationRole> _roleManager,
                                        ApplicationContext db,
                                        IUserProfileService iUserProfileService)
        {
            this._userManager = _userManager;
            this._roleManager = _roleManager;
            this.db = db;
            _iUserProfileService = iUserProfileService;
        }
        #endregion

        #region Actions


        [Authorize]
        [HttpPost("{userId}/complete-profile")]
        public async Task<ActionResult<ApiResponse<string>>> CompleteBasicProfile(string userId, [FromBody] CompleteBasicProfileDTO dto)
        {
            if (CurrentUserId != userId && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
            {
                return Unauthorized(new ApiResponse<string>
                {
                    Code = 401,
                    Status = "Unauthorized",
                    Result = "You are not authorized to complete this profile."
                });
            }

            dto.CreatedBy = CurrentUserName;
            await _iUserProfileService.CompleteBasicProfileAsync(userId, dto);

            return Ok(new ApiResponse<string>
            {
                Code = 200,
                Status = "Success",
                Result = "تم استكمال البيانات بنجاح، وجاري مراجعتها من قبل الإدارة"
            });
        }



        [Authorize]
        [HttpGet("profiles/pending")]
        public async Task<IActionResult> GetPendingProfiles([FromQuery] PaginationFilter filter)
        {
            var result = await _iUserProfileService.GetAllUsersProfilesForAdminAsync(filter);

            return Ok(new ApiResponse<PagedResult<GetUsersProfilesDTO>>
            {
                Code = 200,
                Status = "Success",
                Result = result
            });
        }



        [Authorize]
        [HttpGet("profiles/{userId}")]
        public async Task<ActionResult<ApiResponse<GetUsersProfilesDTO>>> GetUserProfileByUserId(string userId)
        {
            var profile = await _iUserProfileService.GetUserProfileByUserIdAsync(userId);

            return Ok(new ApiResponse<GetUsersProfilesDTO>
            {
                Code = 200,
                Status = "Success",
                Result = profile
            });
        }



        [Authorize]
        [HttpGet("{userId}/status")]
        public async Task<IActionResult> GetUserRequestStatus(string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (currentUserId != userId && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
                return Forbid();

            var status = await _iUserProfileService.GetUserStatusAsync(userId);

            return Ok(new ApiResponse<object>
            {
                Code = 200,
                Status = "Success",
                Result = new
                {
                    UserId = userId,
                    Status = status.ToString()
                }
            });
        }



        [Authorize(Roles = "Admin,Super Admin")]
        [HttpPost("ProfileReviewByAdmin")]
        public async Task<ActionResult<ApiResponse<string>>> ProfileReviewByAdmin([FromBody] ReviewUserProfileByAdminDTO dto)
        {
            if (dto is null)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Code = 400,
                    Status = "BadRequest",
                    Result = "الطلب غير صالح: لا يوجد بيانات مرسلة."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.UserId))
            {
                return BadRequest(new ApiResponse<string>
                {
                    Code = 400,
                    Status = "BadRequest",
                    Result = "معرّف المستخدم مطلوب."
                });
            }

            if (!dto.IsApproved && string.IsNullOrWhiteSpace(dto.RejectionReason))
            {
                return BadRequest(new ApiResponse<string>
                {
                    Code = 400,
                    Status = "BadRequest",
                    Result = "يجب إدخال سبب الرفض إذا لم تتم الموافقة على الملف."
                });
            }

            await _iUserProfileService.ReviewUserProfileByAdminAsync(dto, CurrentUserId, CurrentUserName);

            return Ok(new ApiResponse<string>
            {
                Code = 200,
                Status = "Success",
                Result = dto.IsApproved
                    ? "تمت الموافقة على الملف الشخصي."
                    : "تم رفض الملف الشخصي."
            });
        }



        #endregion


    }
}

using Kindergarten.BLL.Helper;
using Kindergarten.BLL.Models.UserProfileDTO;
using System.Security.Claims;
using Kindergarten.BLL.Services;
using Kindergarten.DAL.Database;
using Kindergarten.DAL.Extend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Kindergarten.BLL.Models;

namespace Kindergarten.API.Controllers
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
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserName = User.FindFirst(ClaimTypes.Name)?.Value;

            if (currentUserId != userId && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
            {
                return Unauthorized(new ApiResponse<string>
                {
                    Code = 401,
                    Status = "Unauthorized",
                    Result = "You are not authorized to complete this profile."
                });
            }

            try
            {
                dto.CreatedBy = currentUserName;

                var result = await _iUserProfileService.CompleteBasicProfileAsync(userId, dto);

                if (!result)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Code = 400,
                        Status = "Failed",
                        Result = "لا يمكنك إنشاء الملف الشخصي أكثر من مرة. تم إرسال بياناتك مسبقًا."
                    });
                }

                return Ok(new ApiResponse<string>
                {
                    Code = 200,
                    Status = "Success",
                    Result = "تم استكمال البيانات بنجاح، وجاري مراجعتها من قبل الإدارة"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Code = 500,
                    Status = "Error",
                    Result = $"حدث خطأ غير متوقع: {ex.Message}"
                });
            }
        }


        //[HttpGet("profilesForAdmin")]
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

            if (profile == null)
            {
                return NotFound(new ApiResponse<string>
                {
                    Code = 404,
                    Status = "Not Found",
                    Result = "لم يتم العثور على الملف الشخصي للمستخدم."
                });
            }

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

            // ✅ التحقق من الصلاحية
            if (currentUserId != userId && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
                return Forbid();

            // ✅ جلب الحالة
            var status = await _iUserProfileService.GetUserStatusAsync(userId);

            // ✅ لم يتم العثور على ملف المستخدم
            if (status == null)
            {
                return NotFound(new ApiResponse<string>
                {
                    Code = 404,
                    Status = "NotFound",
                    Result = "لم يتم العثور على الملف الشخصي للمستخدم."
                });
            }

            // ✅ الرد الناجح
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
        public async Task<IActionResult> ProfileReviewByAdmin([FromBody] ReviewUserProfileByAdminDTO dto)
        {
            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // ✅ التحقق من صحة البيانات
                if (dto == null || string.IsNullOrEmpty(dto.UserId) || (!dto.IsApproved && string.IsNullOrEmpty(dto.RejectionReason)))
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Code = 400,
                        Status = "BadRequest",
                        Result = "البيانات غير صالحة."
                    });
                }

                // ✅ تنفيذ العملية
                var result = await _iUserProfileService.ReviewUserProfileByAdminAsync(dto, currentUserId);

                // ✅ إرجاع النتيجة بناءً على نجاح العملية
                if (result.Success)
                {
                    return Ok(new ApiResponse<string>
                    {
                        Code = 200,
                        Status = "Success",
                        Result = result.Message
                    });
                }
                else
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Code = 400,
                        Status = "Failed",
                        Result = result.Message
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Code = 500,
                    Status = "Error",
                    Result = $"An error occurred: {ex.Message}"
                });
            }
        }

        #endregion


    }
}

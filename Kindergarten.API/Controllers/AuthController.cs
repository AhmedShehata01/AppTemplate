using Kindergarten.BLL.Helper;
using Kindergarten.BLL.Models.Auth;
using Kindergarten.BLL.Services;
using Kindergarten.BLL.Services.SendEmail;
using Kindergarten.DAL.Extend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Kindergarten.API.Controllers
{
    public class AuthController : BaseController
    {
        #region Prop

        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        #endregion

        #region Ctor
        public AuthController(
            IAuthService authService,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService)
        {
            _authService = authService;
            _userManager = userManager;
            _emailService = emailService;
        }

        #endregion

        #region Register & Login

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Code = 400,
                    Status = "InvalidModel",
                    Result = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var result = await _authService.RegisterAsync(model);

            if (result.Code != 200)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Code = result.Code,
                    Status = "RegisterFailed",
                    Result = result.Result
                });
            }

            return Ok(new ApiResponse<string>
            {
                Code = 200,
                Status = "Success",
                Result = result.Result
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Code = 400,
                    Status = "InvalidModel",
                    Result = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var result = await _authService.LoginAsync(model);

            if (result.Code != 200)
            {
                return Unauthorized(new ApiResponse<string>
                {
                    Code = 401,
                    Status = "Unauthorized",
                    Result = result.Result
                });
            }

            return Ok(new ApiResponse<string>
            {
                Code = 200,
                Status = "Success",
                Result = result.Result
            });
        }

        #endregion

        #region Password Management

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Code = 400,
                    Status = "InvalidModel",
                    Result = "بيانات غير صالحة."
                });
            }

            var result = await _authService.ForgotPasswordAsync(model);
            return StatusCode(result.Code, result);
        }


        [Authorize]
        [HttpPost("changePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Code = 400,
                    Status = "InvalidModel",
                    Result = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _authService.ChangePasswordAsync(userId, model);

            return StatusCode(result.Code, result);
        }

        [Authorize]
        [HttpPost("change-password-first-time")]
        public async Task<IActionResult> ChangePasswordFirstTime([FromBody] ChangePasswordFirstTimeDto model)
        {
            if (model.NewPassword != model.ConfirmPassword)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Code = 400,
                    Status = "PasswordMismatch",
                    Result = "كلمة المرور الجديدة غير متطابقة."
                });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<string>
                {
                    Code = 401,
                    Status = "Unauthorized",
                    Result = "تعذر التحقق من هوية المستخدم."
                });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized(new ApiResponse<string>
                {
                    Code = 401,
                    Status = "NotFound",
                    Result = "المستخدم غير موجود."
                });
            }

            var isOldPasswordCorrect = await _userManager.CheckPasswordAsync(user, model.OldPassword);
            if (!isOldPasswordCorrect)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Code = 400,
                    Status = "IncorrectPassword",
                    Result = "كلمة المرور الحالية غير صحيحة."
                });
            }

            if (model.OldPassword == model.NewPassword)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Code = 400,
                    Status = "SamePassword",
                    Result = "لا يمكن استخدام نفس كلمة المرور القديمة."
                });
            }

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Code = 400,
                    Status = "ChangePasswordFailed",
                    Result = result.Errors.Select(e => e.Description).ToList()
                });
            }

            user.IsFirstLogin = false;
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user) ?? new List<string>();
            var token = await _authService.GenerateJwtTokenAsync(user, roles);

            return Ok(new ApiResponse<string>
            {
                Code = 200,
                Status = "Success",
                Result = token
            });
        }

        #endregion

        #region External Login

        [HttpPost("external/google")]
        public async Task<IActionResult> GoogleLogin([FromBody] ExternalLoginRequestDto model)
        {
            var externalUser = await _authService.VerifyGoogleTokenAsync(model.IdToken);

            if (externalUser == null)
            {
                return Unauthorized(new ApiResponse<string>
                {
                    Code = 401,
                    Status = "InvalidToken",
                    Result = "Invalid Google token"
                });
            }

            var authResponse = await _authService.HandleExternalUserAsync(externalUser);
            return Ok(new ApiResponse<object>
            {
                Code = 200,
                Status = "Success",
                Result = authResponse
            });
        }

        [HttpPost("external/facebook")]
        public async Task<IActionResult> FacebookLogin([FromBody] ExternalLoginRequestDto model)
        {
            var externalUser = await _authService.VerifyFacebookTokenAsync(model.IdToken);

            if (externalUser == null)
            {
                return Unauthorized(new ApiResponse<string>
                {
                    Code = 401,
                    Status = "InvalidToken",
                    Result = "Invalid Facebook token"
                });
            }

            var authResponse = await _authService.HandleExternalUserAsync(externalUser);
            return Ok(new ApiResponse<object>
            {
                Code = 200,
                Status = "Success",
                Result = authResponse
            });
        }

        #endregion

        #region User Info

        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var userName = User.Identity?.Name;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            return Ok(new ApiResponse<object>
            {
                Code = 200,
                Status = "Success",
                Result = new { userName, email }
            });
        }

        #endregion
    }
}

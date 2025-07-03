using System.Security.Claims;
using Kindergarten.BLL.Helper;
using Kindergarten.BLL.Models.Auth;
using Kindergarten.BLL.Services;
using Kindergarten.BLL.Services.SendEmail;
using Kindergarten.DAL.Extend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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

            var message = await _authService.RegisterAsync(model); // ❗ ده هيرمي لو حصلت مشكلة

            return Ok(new ApiResponse<string>
            {
                Code = 200,
                Status = "Success",
                Result = message
            });
        }

        #region Old Login
        //[HttpPost("login")]
        //public async Task<IActionResult> Login([FromBody] LoginDTO model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(new ApiResponse<object>
        //        {
        //            Code = 400,
        //            Status = "InvalidModel",
        //            Result = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
        //        });
        //    }

        //    var token = await _authService.LoginAsync(model);

        //    return Ok(new ApiResponse<string>
        //    {
        //        Code = 200,
        //        Status = "Success",
        //        Result = token
        //    });
        //}
        #endregion

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO model)
        {
            // ✅ أول حاجة: نتأكد الـ Model اللي جاي صح
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Code = 400,
                    Status = "InvalidModel",
                    Result = ModelState.Values
                                .SelectMany(v => v.Errors)
                                .Select(e => e.ErrorMessage)
                                .ToList()
                });
            }

            // ✅ ننادي خدمة تسجيل الدخول اللي بتنفذ خطوات:
            // - التحقق من الإيميل والباسورد
            // - لو صح → بيولّد Session للـ OTP
            // - ويبعت OTP للمستخدم
            // - أو بيرجع JWT مباشرة لو مش مطلوب OTP
            var result = await _authService.LoginAsync(model);

            // ✅ لو النتيجة قالت إن OTP مطلوب
            if (result == "OTP_REQUIRED")
            {
                return Ok(new ApiResponse<object>
                {
                    Code = 200,
                    Status = "OtpRequired",
                    Result = new
                    {
                        Message = "Please enter the OTP sent to your registered contact.",
                        Email = model.Email
                    }
                });
            }

            // ✅ لو لأ → معناها إن الـ Login رجع JWT Token
            return Ok(new ApiResponse<string>
            {
                Code = 200,
                Status = "Success",
                Result = result
            });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDTO otpDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Code = 400,
                    Status = "InvalidModel",
                    Result = ModelState.Values
                              .SelectMany(v => v.Errors)
                              .Select(e => e.ErrorMessage)
                              .ToList()
                });
            }

            // ✅ ننادي الخدمة اللي بتتحقق من الـ OTP وترجع الـ JWT
            var token = await _authService.VerifyOtpAndGenerateTokenAsync(otpDto);

            return Ok(new ApiResponse<string>
            {
                Code = 200,
                Status = "Success",
                Result = token
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

            var message = await _authService.ForgotPasswordAsync(model);

            return Ok(new ApiResponse<string>
            {
                Code = 200,
                Status = "Success",
                Result = message
            });
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
            var message = await _authService.ChangePasswordAsync(userId, model);

            return Ok(new ApiResponse<string>
            {
                Code = 200,
                Status = "Success",
                Result = message
            });
        }


        [Authorize]
        [HttpPost("change-password-first-time")]
        public async Task<IActionResult> ChangePasswordFirstTime([FromBody] ChangePasswordFirstTimeDto model)
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
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<string>
                {
                    Code = 401,
                    Status = "Unauthorized",
                    Result = "تعذر التحقق من هوية المستخدم."
                });
            }

            var token = await _authService.ChangePasswordFirstTimeAsync(userId, model);

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

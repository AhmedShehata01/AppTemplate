using Kindergarten.BLL.Helper;
using Kindergarten.BLL.Models.Auth;
using Kindergarten.BLL.Services;
using Kindergarten.BLL.Services.SendEmail;
using Kindergarten.DAL.Extend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Kindergarten.API.Controllers
{
    public class AuthController : BaseController
    {
        #region Prop
        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        #endregion

        #region CTOP
        public AuthController(IAuthService authService,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService)
        {
            _authService = authService;
            _userManager = userManager;
            _emailService = emailService;
        }
        #endregion

        #region Actions 
        // POST: api/auth/register
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

        // POST: api/auth/login
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

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Ok(new ApiResponse<string>
                {
                    Code = 200,
                    Status = "Success",
                    Result = "إذا كان البريد الإلكتروني مسجلاً، سيتم إرسال كلمة مرور جديدة إليه."
                });
            }

            // ✅ توليد كلمة مرور جديدة
            var newPassword = PasswordGenerator.GenerateSecureTemporaryPassword();

            // ✅ توليد Token وإعادة تعيين كلمة المرور
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (!result.Succeeded)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Code = 400,
                    Status = "ResetPasswordFailed",
                    Result = result.Errors.Select(e => e.Description).ToList()
                });
            }

            // ✅ تحديث خصائص المستخدم
            user.IsFirstLogin = true;
            await _userManager.UpdateAsync(user);

            // ✅ بناء محتوى الإيميل بنفس التصميم الجميل
            var emailBody = $@"
                <!DOCTYPE html>
                <html lang=""ar"">
                <head>
                    <meta charset=""UTF-8"">
                    <style>
                        body {{
                            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                            background-color: #f7f7f7;
                            color: #333;
                            direction: rtl;
                            padding: 20px;
                        }}
                        .container {{
                            background-color: #ffffff;
                            border-radius: 8px;
                            padding: 30px;
                            max-width: 600px;
                            margin: auto;
                            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
                        }}
                        .title {{
                            color: #2d89ef;
                            font-size: 24px;
                            margin-bottom: 20px;
                            text-align: center;
                        }}
                        .info {{
                            font-size: 16px;
                            line-height: 1.8;
                            margin-bottom: 25px;
                        }}
                        .highlight {{
                            background-color: #f0f0f0;
                            padding: 10px;
                            border-radius: 5px;
                            font-family: monospace;
                            margin-bottom: 20px;
                        }}
                        .btn {{
                            display: inline-block;
                            background-color: #2d89ef;
                            color: white;
                            padding: 12px 24px;
                            border-radius: 6px;
                            text-decoration: none;
                            font-weight: bold;
                        }}
                        .footer {{
                            margin-top: 30px;
                            font-size: 14px;
                            color: #888;
                            text-align: center;
                        }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""title"">مرحباً {user.Email} 👋</div>

                        <div class=""info"">
                            تم إعادة تعيين كلمة المرور الخاصة بك بنجاح. يمكنك الآن تسجيل الدخول باستخدام البيانات التالية:
                        </div>

                        <div class=""highlight"">
                            <div><strong>البريد الإلكتروني:</strong> {user.Email}</div>
                            <div><strong>كلمة المرور الجديدة:</strong> {newPassword}</div>
                        </div>

                        <div style=""text-align: center; margin-bottom: 20px;"">
                            <a href=""{model.LoginUrl}"" class=""btn"">تسجيل الدخول الآن</a>
                        </div>

                        <div class=""info"">
                            تأكد من تغيير كلمة المرور بعد الدخول حفاظاً على أمان حسابك.
                        </div>

                        <div class=""footer"">
                            هذا البريد تم إرساله تلقائيًا من نظام إدارة الحضانة.
                        </div>
                    </div>
                </body>
                </html>";

            // ✅ إرسال الإيميل
            await _emailService.SendEmailAsync(user.Email, "إعادة تعيين كلمة المرور - نظام الحضانة", emailBody);

            return Ok(new ApiResponse<string>
            {
                Code = 200,
                Status = "Success",
                Result = "تم إرسال كلمة مرور جديدة إلى بريدك الإلكتروني."
            });
        }

        // POST: /api/auth/change-password
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

        // AuthController.cs
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

        // POST: api/auth/external/facebook
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

        // GET: api/auth/me
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

            // تحقق من صحة كلمة المرور الحالية
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

            // ✅ تحقق من أن كلمة المرور الجديدة ليست نفس القديمة
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

            // تحديث حالة المستخدم بعد تغيير كلمة المرور
            user.IsFirstLogin = false;
            await _userManager.UpdateAsync(user);

            // الحصول على الأدوار وتوليد التوكن
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
    }

}

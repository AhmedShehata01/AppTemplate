using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Kindergarten.BLL.Helper;
using Kindergarten.BLL.Models.ActivityLogDTO;
using Kindergarten.BLL.Models.Auth;
using Kindergarten.BLL.Services.SendEmail;
using Kindergarten.DAL.Database;
using Kindergarten.DAL.Enum;
using Kindergarten.DAL.Extend;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Kindergarten.BLL.Services
{
    public class AuthService : IAuthService
    {
        #region Prop
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;
        private readonly ApplicationContext _context;
        private readonly IActivityLogService _activityLogService;
        private readonly IEmailService _emailService;
        private readonly string _googleClientId;
        private readonly string _facebookAppId;
        private readonly string _facebookAppSecret;
        #endregion

        #region Ctor
        public AuthService(
            UserManager<ApplicationUser> userManager,
            IConfiguration config,
            ApplicationContext context,
            IActivityLogService activityLogService,
            IEmailService emailService)
        {
            _userManager = userManager;
            _config = config;
            _context = context;
            _activityLogService = activityLogService;
            _emailService = emailService;
            _googleClientId = _config["Google:ClientId"];
            _facebookAppId = _config["Facebook:AppId"];
            _facebookAppSecret = _config["Facebook:AppSecret"];
        }

        #endregion

        #region Auth Actions

        public async Task<ApiResponse<string>> RegisterAsync(RegisterDTO model)
        {
            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new ApiResponse<string>
                {
                    Code = 400,
                    Status = "Failed",
                    Result = errors
                };
            }

            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = nameof(ApplicationUser),
                EntityId = user.Id,
                ActionType = ActivityActionType.Register,
                SystemComment = $"User '{user.UserName}' registered successfully.",
                PerformedByUserId = user.Id,
                PerformedByUserName = user.UserName
            });

            return new ApiResponse<string>
            {
                Code = 200,
                Status = "Success",
                Result = "User registered successfully"
            };
        }

        public async Task<ApiResponse<string>> LoginAsync(LoginDTO model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                return new ApiResponse<string>
                {
                    Code = 401,
                    Status = "Failed",
                    Result = "Invalid credentials"
                };
            }

            var roles = await _userManager.GetRolesAsync(user);
            var jwtToken = await GenerateJwtTokenAsync(user, roles);

            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = nameof(ApplicationUser),
                EntityId = user.Id,
                ActionType = ActivityActionType.Login,
                SystemComment = $"User '{user.UserName}' logged in successfully.",
                PerformedByUserId = user.Id,
                PerformedByUserName = user.UserName
            });

            return new ApiResponse<string>
            {
                Code = 200,
                Status = "Success",
                Result = jwtToken
            };
        }

        public async Task<ApiResponse<string>> ChangePasswordAsync(string userId, ChangePasswordDTO model)
        {
            if (model.NewPassword == model.CurrentPassword)
            {
                return new ApiResponse<string>
                {
                    Code = 400,
                    Status = "Failed",
                    Result = "New password must be different from current password"
                };
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new ApiResponse<string>
                {
                    Code = 404,
                    Status = "Failed",
                    Result = "User not found"
                };
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new ApiResponse<string>
                {
                    Code = 400,
                    Status = "Failed",
                    Result = errors
                };
            }

            // ✅ Log after successful password change
            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = nameof(ApplicationUser),
                EntityId = user.Id,
                ActionType = ActivityActionType.ChangePassword,
                SystemComment = $"User '{user.UserName}' changed password successfully.",
                PerformedByUserId = user.Id,
                PerformedByUserName = user.UserName
            });

            return new ApiResponse<string>
            {
                Code = 200,
                Status = "Success",
                Result = "Password changed successfully"
            };
        }

        public async Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return new ApiResponse<string>
                {
                    Code = 200,
                    Status = "Success",
                    Result = "إذا كان البريد الإلكتروني مسجلاً، سيتم إرسال كلمة مرور جديدة إليه."
                };
            }

            var newPassword = PasswordGenerator.GenerateSecureTemporaryPassword();
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return new ApiResponse<string>
                {
                    Code = 400,
                    Status = "ResetPasswordFailed",
                    Result = string.Join(", ", errors)
                };
            }

            user.IsFirstLogin = true;
            await _userManager.UpdateAsync(user);

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

            await _emailService.SendEmailAsync(
                user.Email,
                "إعادة تعيين كلمة المرور - نظام الحضانة",
                emailBody);

            return new ApiResponse<string>
            {
                Code = 200,
                Status = "Success",
                Result = "تم إرسال كلمة مرور جديدة إلى بريدك الإلكتروني."
            };
        }

        public async Task<string> GenerateJwtTokenAsync(ApplicationUser user, IList<string> roles)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["JWT:Key"]);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim("IsAgree", user.IsAgree ? "true" : "false")
            };

            if (user.IsFirstLogin)
            {
                claims.Add(new Claim("IsFirstLogin", "true"));
            }

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var securedRoutes = await _context.RoleSecuredRoutes
                .Where(r => roles.Contains(r.Role.Name))
                .Select(r => r.SecuredRoute.BasePath)
                .Distinct()
                .ToListAsync();

            foreach (var route in securedRoutes)
            {
                claims.Add(new Claim("SecuredRoute", route));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["JWT:DurationInMinutes"])),
                Issuer = _config["JWT:Issuer"],
                Audience = _config["JWT:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        #endregion

        #region External Login

        public async Task<ExternalUserInfoDto?> VerifyGoogleTokenAsync(string idToken)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new List<string> { _googleClientId }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

                return new ExternalUserInfoDto
                {
                    Provider = "Google",
                    ProviderUserId = payload.Subject,
                    Email = payload.Email,
                    FullName = payload.Name
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("Google Token Validation Failed:");
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        public async Task<ExternalUserInfoDto?> VerifyFacebookTokenAsync(string accessToken)
        {
            var client = new HttpClient();
            var appAccessToken = $"{_facebookAppId}|{_facebookAppSecret}";

            var verifyTokenUrl = $"https://graph.facebook.com/debug_token?input_token={accessToken}&access_token={appAccessToken}";
            var verifyResponse = await client.GetAsync(verifyTokenUrl);

            if (!verifyResponse.IsSuccessStatusCode)
                return null;

            var userInfoUrl = $"https://graph.facebook.com/me?fields=id,name,email&access_token={accessToken}";
            var userInfoResponse = await client.GetAsync(userInfoUrl);

            if (!userInfoResponse.IsSuccessStatusCode)
                return null;

            var content = await userInfoResponse.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content).RootElement;

            return new ExternalUserInfoDto
            {
                Provider = "Facebook",
                ProviderUserId = json.GetProperty("id").GetString(),
                Email = json.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null,
                FullName = json.GetProperty("name").GetString()
            };
        }

        public async Task<AuthResponseDto> HandleExternalUserAsync(ExternalUserInfoDto externalUser)
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u =>
                    u.Provider == externalUser.Provider &&
                    u.ProviderUserId == externalUser.ProviderUserId);

            bool isNewUser = false;

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = externalUser.Email ?? Guid.NewGuid().ToString(),
                    Email = externalUser.Email,
                    FullName = externalUser.FullName,
                    Provider = externalUser.Provider,
                    ProviderUserId = externalUser.ProviderUserId,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to create user: {errors}");
                }

                isNewUser = true;
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = await GenerateJwtTokenAsync(user, roles);

            // ✅ سجل الـ ActivityLog حسب الحالة
            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = nameof(ApplicationUser),
                EntityId = user.Id,
                ActionType = isNewUser ? ActivityActionType.FirstExternalLogin : ActivityActionType.ExternalLogin,
                SystemComment = isNewUser
                    ? $"User '{user.UserName}' registered via {externalUser.Provider}."
                    : $"User '{user.UserName}' logged in via {externalUser.Provider}.",
                PerformedByUserId = user.Id,
                PerformedByUserName = user.UserName
            });

            return new AuthResponseDto
            {
                IsAuthenticated = true,
                Token = token,
                Email = user.Email,
                UserName = user.UserName,
                FullName = user.FullName,
                Provider = user.Provider
            };
        }


        #endregion
    }

    public interface IAuthService
    {
        #region Auth Actions

        Task<ApiResponse<string>> RegisterAsync(RegisterDTO model);
        Task<ApiResponse<string>> LoginAsync(LoginDTO model);
        Task<ApiResponse<string>> ChangePasswordAsync(string userId, ChangePasswordDTO model);
        Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordDto model);
        Task<string> GenerateJwtTokenAsync(ApplicationUser user, IList<string> roles);

        #endregion

        #region External Login

        Task<ExternalUserInfoDto?> VerifyGoogleTokenAsync(string accessToken);
        Task<ExternalUserInfoDto?> VerifyFacebookTokenAsync(string accessToken);
        Task<AuthResponseDto> HandleExternalUserAsync(ExternalUserInfoDto externalUser);

        #endregion
    }
}

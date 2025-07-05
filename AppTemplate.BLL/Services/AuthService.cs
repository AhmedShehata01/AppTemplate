using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Google.Apis.Auth;
using AppTemplate.BLL.Helper;
using AppTemplate.BLL.Models.ActivityLogDTO;
using AppTemplate.BLL.Models.Auth;
using AppTemplate.BLL.Services.SendEmail;
using AppTemplate.DAL.Database;
using AppTemplate.DAL.Enum;
using AppTemplate.DAL.Extend;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using AppTemplate.BLL.Models;


namespace AppTemplate.BLL.Services
{
    public class AuthService : IAuthService
    {
        #region Prop
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;
        private readonly ApplicationContext _context;
        private readonly IActivityLogService _activityLogService;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;
        private readonly IOtpService _otpService;
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
            IEmailService emailService,
            IMemoryCache cache,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuthService> logger,
            IOtpService otpService)
        {
            _userManager = userManager;
            _config = config;
            _context = context;
            _activityLogService = activityLogService;
            _emailService = emailService;
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _otpService = otpService;
            _googleClientId = _config["Google:ClientId"];
            _facebookAppId = _config["Facebook:AppId"];
            _facebookAppSecret = _config["Facebook:AppSecret"];
        }
        #endregion


        #region Auth Actions

        public async Task<string> RegisterAsync(RegisterDTO model)
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
                throw new InvalidOperationException(errors); // ❗ أو CustomException لو حبيت
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

            return "User registered successfully";
        }

        #region Login With Otp
        public async Task<ActionResultDTO<string>> LoginAsync(LoginDTO model)
        {
            var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

            var cacheKey = $"login-attempts-{model.Email.ToLower()}-{ipAddress}";
            var ipKey = $"ip-login-attempts-{ipAddress}";
            var ipAlertedKey = $"{ipKey}-alerted";

            var attempts = _cache.Get<int>(cacheKey);
            var ipAttempts = _cache.Get<int>(ipKey);
            var alreadyAlerted = _cache.Get<bool>(ipAlertedKey);

            if (attempts >= 5)
            {
                return new ActionResultDTO<string>
                {
                    Success = false,
                    Message = "Too many login attempts. Please try again later.",
                    Data = null
                };
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var newAttempts = attempts + 1;
                _cache.Set(cacheKey, newAttempts, TimeSpan.FromMinutes(5));

                var newIpAttempts = ipAttempts + 1;
                _cache.Set(ipKey, newIpAttempts, TimeSpan.FromMinutes(5));

                if (!alreadyAlerted && newIpAttempts >= 10)
                {
                    var adminEmail = _config["AdminSettings:NotificationEmail"];
                    var emailBody = "..."; // body زي ما هو
                    await _emailService.SendEmailAsync(
                        adminEmail,
                        "تحذير أمني: محاولات تسجيل دخول مريبة",
                        emailBody
                    );
                    _cache.Set(ipAlertedKey, true, TimeSpan.FromMinutes(10));
                }

                return new ActionResultDTO<string>
                {
                    Success = false,
                    Message = "Invalid credentials",
                    Data = null
                };
            }

            _cache.Remove(cacheKey);

            string loginSessionKey = $"login-session-{user.Id}";
            _cache.Set(loginSessionKey, true, TimeSpan.FromMinutes(5));

            var otpResult = await _otpService.GenerateAndSendOtpAsync(
                new RequestOtpDTO
                {
                    Email = user.Email,
                    Purpose = OtpPurpose.Login
                });

            if (!otpResult.Success)
            {
                return new ActionResultDTO<string>
                {
                    Success = false,
                    Message = otpResult.Message,
                    Data = null
                };
            }

            return new ActionResultDTO<string>
            {
                Success = true,
                Message = "OTP_REQUIRED",
                Data = null
            };
        }


        public async Task<ActionResultDTO<string>> VerifyOtpAndGenerateTokenAsync(VerifyOtpDTO otpDto)
        {
            var user = await _userManager.FindByEmailAsync(otpDto.Email);
            if (user == null)
            {
                return new ActionResultDTO<string>
                {
                    Success = false,
                    Message = "User not found",
                    Data = null
                };
            }

            string loginSessionKey = $"login-session-{user.Id}";
            var sessionExists = _cache.Get<bool>(loginSessionKey);
            if (!sessionExists)
            {
                return new ActionResultDTO<string>
                {
                    Success = false,
                    Message = "You must login first and enter your password.",
                    Data = null
                };
            }

            var verifyResult = await _otpService.VerifyOtpAsync(
                new VerifyOtpDTO { Email = otpDto.Email, Code = otpDto.Code }
            );
            if (!verifyResult.Success)
            {
                return new ActionResultDTO<string>
                {
                    Success = false,
                    Message = verifyResult.Message,
                    Data = null
                };
            }

            _cache.Remove(loginSessionKey);

            var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
            string userAttemptsKey = $"login-attempts-{user.Email.ToLower()}-{ipAddress}";
            string ipAttemptsKey = $"ip-login-attempts-{ipAddress}";

            _cache.Remove(userAttemptsKey);
            _cache.Remove(ipAttemptsKey);

            var roles = await _userManager.GetRolesAsync(user);
            var jwtToken = await GenerateJwtTokenAsync(user, roles);

            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = nameof(ApplicationUser),
                EntityId = user.Id,
                ActionType = ActivityActionType.Login,
                SystemComment = $"User '{user.UserName}' logged in using OTP successfully.",
                PerformedByUserId = user.Id,
                PerformedByUserName = user.UserName
            });

            return new ActionResultDTO<string>
            {
                Success = true,
                Message = "Login successful",
                Data = jwtToken
            };
        }





        #endregion


        public async Task<string> ChangePasswordAsync(string userId, ChangePasswordDTO model)
        {
            if (model.NewPassword == model.CurrentPassword)
            {
                throw new InvalidOperationException("New password must be different from current password.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException(errors);
            }

            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = nameof(ApplicationUser),
                EntityId = user.Id,
                ActionType = ActivityActionType.ChangePassword,
                SystemComment = $"User '{user.UserName}' changed password successfully.",
                PerformedByUserId = user.Id,
                PerformedByUserName = user.UserName
            });

            return "Password changed successfully";
        }

        public async Task<string> ChangePasswordFirstTimeAsync(string userId, ChangePasswordFirstTimeDto model)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            var isOldPasswordCorrect = await _userManager.CheckPasswordAsync(user, model.OldPassword);
            if (!isOldPasswordCorrect)
                throw new UnauthorizedAccessException("كلمة المرور الحالية غير صحيحة.");

            if (model.OldPassword == model.NewPassword)
                throw new InvalidOperationException("لا يمكن استخدام نفس كلمة المرور القديمة.");

            if (model.NewPassword != model.ConfirmPassword)
                throw new InvalidOperationException("كلمة المرور الجديدة غير متطابقة.");

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                throw new InvalidOperationException(string.Join(", ", errors));
            }

            user.IsFirstLogin = false;
            user.IsAgree = true;
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user) ?? new List<string>();
            var token = await GenerateJwtTokenAsync(user, roles);

            return token;
        }


        public async Task<string> ForgotPasswordAsync(ForgotPasswordDto model)
        {
            var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

            // مفتاح تتبع محاولات نسيت كلمة المرور للبريد + IP
            var cacheKey = $"forgot-password-attempts-{model.Email.ToLower()}-{ipAddress}";

            // مفتاح علم إرسال التنبيه
            var alertSentKey = $"{cacheKey}-alerted";

            var attempts = _cache.Get<int>(cacheKey);
            var alertSent = _cache.Get<bool>(alertSentKey);

            // لو IP حاول أكتر من 5 مرات → ابعت إيميل تنبيه
            if (attempts >= 5)
            {
                // يمكن تحذير المستخدم برفق أو إعطاء رسالة عامة
                return "لقد تجاوزت الحد المسموح به لطلبات إعادة تعيين كلمة المرور، الرجاء المحاولة بعد قليل.";
            }

            // زوّد عدد المحاولات
            attempts++;
            _cache.Set(cacheKey, attempts, TimeSpan.FromMinutes(5));

            // لو عدد المحاولات تجاوز 5 و التنبيه مش مرسل
            if (attempts >= 5 && !alertSent)
            {
                _logger.LogWarning(
                    "🚨 تم تنفيذ طلبات نسيت كلمة المرور أكثر من 5 مرات على البريد {Email} من IP {IP} خلال 5 دقائق.",
                    model.Email,
                    ipAddress
                );

                var adminEmail = _config["AdminSettings:NotificationEmail"];

                var adminEmailBody = $@"
                                <!DOCTYPE html>
                                <html lang=""ar"">
                                <head>
                                    <meta charset=""UTF-8"">
                                    <style>
                                        body {{
                                            font-family: Tahoma, Arial, sans-serif;
                                            background-color: #f9f9f9;
                                            color: #333;
                                            padding: 20px;
                                            direction: rtl;
                                        }}
                                        .container {{
                                            background-color: #fff;
                                            border-radius: 8px;
                                            padding: 25px;
                                            max-width: 600px;
                                            margin: auto;
                                            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
                                        }}
                                        .title {{
                                            font-size: 20px;
                                            font-weight: bold;
                                            color: #d9534f;
                                            margin-bottom: 15px;
                                            text-align: center;
                                        }}
                                        .details {{
                                            font-size: 15px;
                                            line-height: 1.7;
                                        }}
                                        .highlight {{
                                            background-color: #f2f2f2;
                                            padding: 8px;
                                            border-radius: 5px;
                                            display: inline-block;
                                            margin-top: 10px;
                                            font-weight: bold;
                                        }}
                                        .footer {{
                                            margin-top: 25px;
                                            font-size: 13px;
                                            color: #888;
                                            text-align: center;
                                        }}
                                    </style>
                                </head>
                                <body>
                                    <div class=""container"">
                                        <div class=""title"">
                                            🚨 تحذير أمني من نظام الحضانة
                                        </div>
                                        <div class=""details"">
                                            <p>تم تنفيذ أكثر من <strong>5 محاولات نسيت كلمة المرور</strong> على البريد الإلكتروني التالي من عنوان IP:</p>
                                            <div class=""highlight"">{model.Email}</div>

                                            <p>عنوان IP:</p>
                                            <div class=""highlight"">{ipAddress}</div>

                                            <p>عدد المحاولات خلال آخر 5 دقائق:</p>
                                            <div class=""highlight"">{attempts}</div>

                                            <p>يرجى التحقق من هذا النشاط لأنه قد يكون محاولة اختراق.</p>
                                        </div>
                                        <div class=""footer"">
                                            هذا البريد تم إرساله تلقائيًا من نظام إدارة الحضانة.
                                        </div>
                                    </div>
                                </body>
                                </html>";


                await _emailService.SendEmailAsync(
                    adminEmail,
                    "تحذير أمني: محاولات نسيت كلمة المرور مريبة",
                    adminEmailBody);

                // علم أنه تم إرسال التنبيه حتى لا يرسل أكثر من مرة
                _cache.Set(alertSentKey, true, TimeSpan.FromMinutes(10));
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // نرجع نفس الرسالة بدون كشف معلومات
                return "إذا كان البريد الإلكتروني مسجلاً، سيتم إرسال كلمة مرور جديدة إليه.";
            }

            var newPassword = PasswordGenerator.GenerateSecureTemporaryPassword();
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                throw new InvalidOperationException(string.Join(", ", errors));
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

            return "تم إرسال كلمة مرور جديدة إلى بريدك الإلكتروني.";
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

        Task<string> RegisterAsync(RegisterDTO model);
        Task<ActionResultDTO<string>> LoginAsync(LoginDTO model);
        Task<ActionResultDTO<string>> VerifyOtpAndGenerateTokenAsync(VerifyOtpDTO otpDto);
        Task<string> ChangePasswordAsync(string userId, ChangePasswordDTO model);
        Task<string> ForgotPasswordAsync(ForgotPasswordDto model);
        Task<string> GenerateJwtTokenAsync(ApplicationUser user, IList<string> roles);
        Task<string> ChangePasswordFirstTimeAsync(string userId, ChangePasswordFirstTimeDto model);

        #endregion

        #region External Login

        Task<ExternalUserInfoDto?> VerifyGoogleTokenAsync(string accessToken);
        Task<ExternalUserInfoDto?> VerifyFacebookTokenAsync(string accessToken);
        Task<AuthResponseDto> HandleExternalUserAsync(ExternalUserInfoDto externalUser);

        #endregion
    }
}

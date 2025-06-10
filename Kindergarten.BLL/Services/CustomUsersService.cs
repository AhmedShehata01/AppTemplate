using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Kindergarten.BLL.Models.UsersManagementDTO;
using Kindergarten.BLL.Services.SendEmail;
using Kindergarten.DAL.Extend;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Kindergarten.BLL.Services
{
    public class CustomUsersService : ICustomUsersService
    {
        #region Prop
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IEmailService _emailService; // إنت ممكن تبنيه أو تستخدم موجود
        private readonly IHttpContextAccessor _httpContextAccessor;
        #endregion

        #region CTOR
        public CustomUsersService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IEmailService emailService,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
        }
        #endregion

        #region Actions
        public async Task<(string UserId, bool EmailSent)> CreateUserByAdminAsync(CreateUserByAdminDTO dto)
        {
            // 1. تحقق من صلاحية المستخدم الحالي
            var currentUser = _httpContextAccessor.HttpContext?.User;
            if (currentUser == null || (!currentUser.IsInRole("Admin") && !currentUser.IsInRole("SuperAdmin")))
                throw new UnauthorizedAccessException("You are not authorized to create users.");

            // 2. إنشاء المستخدم
            var fullName = $"{dto.FirstName} {dto.LastName}";
            var user = new ApplicationUser
            {
                UserName = fullName,
                NormalizedUserName = fullName.ToUpper(),
                Email = dto.Email,
                NormalizedEmail = dto.Email.ToUpper(),
                PhoneNumber = dto.PhoneNumber,
                EmailConfirmed = true,
                IsAgree = false,
                CreatedOn = DateTime.UtcNow
            };

            // ✅ 3. توليد Password مؤقتة
            var tempPassword = GenerateSecureTemporaryPassword(); // هنجهزه تحت

            // 4. إنشاء المستخدم بكلمة المرور المؤقتة
            var result = await _userManager.CreateAsync(user, tempPassword);
            if (!result.Succeeded)
                throw new Exception($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");

            // 5. تعيين الأدوار
            if (dto.Roles.Any())
            {
                var addToRolesResult = await _userManager.AddToRolesAsync(user, dto.Roles);
                if (!addToRolesResult.Succeeded)
                    throw new Exception($"Failed to assign roles: {string.Join(", ", addToRolesResult.Errors.Select(e => e.Description))}");
            }

            // ✅ 6. إعداد محتوى الإيميل
            var loginUrl = dto.RedirectUrlAfterResetPassword; // ممكن تسميه LoginUrl أو تخليه مخصص
                                                              // ✅ 6. إعداد محتوى الإيميل (مودرن HTML)
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
                            تم إنشاء حساب جديد لك في نظام الحضانة. يمكنك تسجيل الدخول باستخدام البيانات التالية:
                        </div>

                        <div class=""highlight"">
                            <div><strong>البريد الإلكتروني:</strong> {user.Email}</div>
                            <div><strong>كلمة المرور المؤقتة:</strong> {tempPassword}</div>
                        </div>

                        <div style=""text-align: center; margin-bottom: 20px;"">
                            <a href=""{loginUrl}"" class=""btn"">تسجيل الدخول الآن</a>
                        </div>

                        <div class=""info"">
                            يرجى تغيير كلمة المرور بعد تسجيل الدخول مباشرة لضمان أمان حسابك.
                        </div>

                        <div class=""footer"">
                            هذا البريد تم إرساله تلقائيًا من نظام إدارة الحضانة.
                        </div>
                    </div>
                </body>
                </html>";


            // 7. إرسال الإيميل
            await _emailService.SendEmailAsync(user.Email, "بيانات الدخول إلى نظام الحضانة", emailBody);

            return (user.Id, true);
        }

        private string GenerateSecureTemporaryPassword()
        {
            var rng = new byte[4];
            using (var generator = RandomNumberGenerator.Create())
            {
                generator.GetBytes(rng);
            }
            int number = BitConverter.ToInt32(rng, 0) % 90000 + 10000; // رقم من 10000 إلى 99999
            return $"Aa@{Math.Abs(number)}";
        }

        #endregion
    }

    public interface ICustomUsersService
    {
        Task<(string UserId, bool EmailSent)> CreateUserByAdminAsync(CreateUserByAdminDTO dto);

    }
}

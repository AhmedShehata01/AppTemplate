using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Kindergarten.BLL.Helper;
using Kindergarten.BLL.Models.UsersManagementDTO;
using Kindergarten.BLL.Services.SendEmail;
using Kindergarten.DAL.Database;
using Kindergarten.DAL.Entity;
using Kindergarten.DAL.Enum;
using Kindergarten.DAL.Extend;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kindergarten.BLL.Services
{
    public class CustomUsersService : ICustomUsersService
    {
        #region Prop
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IEmailService _emailService; // إنت ممكن تبنيه أو تستخدم موجود
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMapper _mapper;
        private readonly ApplicationContext _db;
        private readonly IOptions<AdminSettings> _adminSettings;
        #endregion

        #region CTOR
        public CustomUsersService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IEmailService emailService,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper, 
            ApplicationContext db,
            IOptions<AdminSettings> adminSettings)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
            _mapper = mapper;
            _db = db;
            _adminSettings = adminSettings;
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
                CreatedOn = DateTime.UtcNow,
                IsFirstLogin = true
            };

            // ✅ 3. توليد Password مؤقتة
            var tempPassword = PasswordGenerator.GenerateSecureTemporaryPassword(); // هنجهزه تحت

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

        public async Task<bool> CompleteBasicProfileAsync(string userId, CompleteBasicProfileDTO dto)
        {
            var existingProfile = await _db.UserBasicProfiles.FindAsync(userId);

            if (existingProfile != null && existingProfile.Status != UserStatus.rejected)
            {
                // لا يُسمح بإنشاء أو تعديل الملف الشخصي إذا لم يكن مرفوضًا
                return false;
            }

            if (existingProfile != null && existingProfile.Status == UserStatus.rejected)
            {
                // تعديل الملف المرفوض
                _mapper.Map(dto, existingProfile); // تحديث الخصائص من الـ DTO إلى الـ Profile
                existingProfile.Status = UserStatus.pendingApproval;
                existingProfile.SubmittedAt = DateTime.UtcNow;

                _db.UserBasicProfiles.Update(existingProfile);
            }
            else
            {
                // إنشاء ملف جديد
                var profile = _mapper.Map<UserBasicProfile>(dto);
                profile.UserId = userId;
                profile.Status = UserStatus.pendingApproval;
                profile.SubmittedAt = DateTime.UtcNow;

                _db.UserBasicProfiles.Add(profile);
            }

            var result = await _db.SaveChangesAsync();

            if (result > 0)
            {
                // إرسال إشعار إلى الإدارة
                var adminEmail = _adminSettings.Value.NotificationEmail;
                var user = await _userManager.FindByIdAsync(userId);

                var emailBody = $@"
                    <html dir='rtl'>
                    <body style='font-family: Tahoma, sans-serif; background-color: #f9f9f9; padding: 20px;'>
                        <div style='background-color: #fff; padding: 20px; border-radius: 10px; border: 1px solid #ddd;'>
                            <h2 style='color: #2d89ef;'>طلب مراجعة بيانات موظف جديد</h2>
                            <p>قام الموظف التالي باستكمال بياناته الشخصية ويحتاج إلى مراجعة وموافقة الإدارة:</p>
                            <ul style='line-height: 1.8;'>
                                <li><strong>الاسم:</strong> {user.UserName}</li>
                                <li><strong>البريد الإلكتروني:</strong> {user.Email}</li>
                                <li><strong>رقم الهاتف:</strong> {user.PhoneNumber}</li>
                            </ul>
                            <p>يرجى مراجعة حسابه من لوحة التحكم وتفعيله إذا كانت البيانات صحيحة.</p>
                        </div>
                    </body>
                    </html>";

                await _emailService.SendEmailAsync(adminEmail, "طلب مراجعة موظف جديد", emailBody);

                return true;
            }

            return false;
        }

        public async Task<UserStatus?> GetUserStatusAsync(string userId)
        {
            var profile = await _db.UserBasicProfiles
                .Where(p => p.UserId == userId)
                .Select(p => p.Status)
                .FirstOrDefaultAsync();

            return profile;
        }

        public async Task<ActionResultDTO> ReviewUserProfileByAdminAsync(ReviewUserProfileByAdminDTO dto, string reviewedById)
        {
            // 🟡 جلب الملف الشخصي للمستخدم من قاعدة البيانات
            var profile = await _db.UserBasicProfiles.FirstOrDefaultAsync(p => p.UserId == dto.UserId);
            if (profile == null)
            {
                return new ActionResultDTO
                {
                    Success = false,
                    Message = "لم يتم العثور على بيانات الموظف."
                };
            }

            // 🟡 جلب بيانات المستخدم من نظام الهوية (ASP.NET Identity)
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
            {
                return new ActionResultDTO
                {
                    Success = false,
                    Message = "المستخدم غير موجود."
                };
            }

            // 🟢 تحديث بيانات المراجعة في الملف الشخصي
            profile.Status = dto.IsApproved ? UserStatus.approved : UserStatus.rejected;
            profile.RejectionReason = dto.IsApproved ? null : dto.RejectionReason;
            profile.ReviewedBy = reviewedById;
            profile.ReviewedAt = DateTime.UtcNow;

            // ✅ إرفاق الكيان في الـ DbContext وتحديد الخصائص المعدلة يدويًا لضمان تتبعها
            _db.Attach(profile);
            var entry = _db.Entry(profile);
            entry.Property(p => p.Status).IsModified = true;
            entry.Property(p => p.RejectionReason).IsModified = true;
            entry.Property(p => p.ReviewedBy).IsModified = true;
            entry.Property(p => p.ReviewedAt).IsModified = true;

            // 🟢 في حالة الموافقة: تحديث خاصية IsAgree في حساب المستخدم (AspNetUsers)
            if (dto.IsApproved && !user.IsAgree)
            {
                user.IsAgree = true;

                var updateUserResult = await _userManager.UpdateAsync(user);
                if (!updateUserResult.Succeeded)
                {
                    return new ActionResultDTO
                    {
                        Success = false,
                        Message = "حدث خطأ أثناء تحديث بيانات المستخدم."
                    };
                }
            }

            // 💾 حفظ التغييرات على كيان الملف الشخصي فقط (وليس حساب المستخدم)
            await _db.SaveChangesAsync();
            // ⚠️ لا يتم الاعتماد على القيمة المرجعة من SaveChangesAsync() لأنها لا تشمل تغييرات UserManager

            // ✉️ إرسال بريد إلكتروني للمستخدم لإعلامه بنتيجة المراجعة
            if (!string.IsNullOrEmpty(user.Email))
            {
                var subject = dto.IsApproved ? "تمت الموافقة على حسابك ✅" : "تم رفض حسابك ❌";

                var message = dto.IsApproved
                    ? "<p>نود إعلامك أنه تمت مراجعة بياناتك من قبل الإدارة، وتم تفعيل حسابك بنجاح.</p>"
                    : $@"<p>نأسف لإبلاغك أنه تم رفض طلب تفعيل حسابك.</p>
                 <p><strong>سبب الرفض:</strong> {dto.RejectionReason}</p>";

                var emailBody = $@"
                    <!DOCTYPE html>
                    <html lang=""ar"" dir=""rtl"">
                    <head>
                        <meta charset=""UTF-8"">
                        <style>
                            body {{
                                font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                                background-color: #f4f4f4;
                                padding: 20px;
                                color: #333;
                            }}
                            .container {{
                                background-color: #fff;
                                border-radius: 10px;
                                padding: 30px;
                                max-width: 600px;
                                margin: auto;
                                box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
                            }}
                            .title {{
                                font-size: 22px;
                                margin-bottom: 20px;
                                color: {(dto.IsApproved ? "#28a745" : "#dc3545")};
                            }}
                            .footer {{
                                margin-top: 30px;
                                font-size: 14px;
                                color: #777;
                                text-align: center;
                            }}
                        </style>
                    </head>
                    <body>
                        <div class=""container"">
                            <div class=""title"">{subject}</div>
                            <div class=""content"">{message}</div>
                            <div class=""footer"">هذا البريد تم إرساله تلقائيًا من نظام إدارة الحضانة</div>
                        </div>
                    </body>
                    </html>";

                await _emailService.SendEmailAsync(user.Email, subject, emailBody);
            }

            // ✅ النتيجة النهائية
            return new ActionResultDTO
            {
                Success = true,
                Message = dto.IsApproved
                    ? "تمت الموافقة على الملف الشخصي."
                    : "تم رفض الملف الشخصي."
            };
        }

        // get all Users Profiles 
        public async Task<List<GetUsersProfilesDTO>> GetAllUsersProfilesForAdminAsync()
        {
            var profiles = await _db.UserBasicProfiles
                .Include(p => p.User)
                .Where(p => p.IsDeleted == false && p.Status == UserStatus.pendingApproval)
                .ToListAsync();
            var result = _mapper.Map<List<GetUsersProfilesDTO>>(profiles);
            return result;
        }

        public async Task<GetUsersProfilesDTO?> GetUserProfileByUserIdAsync(string userId)
        {
            var profile = await _db.UserBasicProfiles
                .Include(p => p.User)
                .Where(p => p.IsDeleted == false && p.UserId == userId)
                .FirstOrDefaultAsync();

            if (profile == null)
                return null;

            var result = _mapper.Map<GetUsersProfilesDTO>(profile);
            return result;
        }

        #endregion
    }

    public interface ICustomUsersService
    {
        Task<(string UserId, bool EmailSent)> CreateUserByAdminAsync(CreateUserByAdminDTO dto);
        Task<bool> CompleteBasicProfileAsync(string userId, CompleteBasicProfileDTO dto);
        Task<UserStatus?> GetUserStatusAsync(string userId);
        Task<ActionResultDTO> ReviewUserProfileByAdminAsync(ReviewUserProfileByAdminDTO dto, string reviewedById);
        Task<List<GetUsersProfilesDTO>> GetAllUsersProfilesForAdminAsync();
        Task<GetUsersProfilesDTO?> GetUserProfileByUserIdAsync(string userId);
    }
}

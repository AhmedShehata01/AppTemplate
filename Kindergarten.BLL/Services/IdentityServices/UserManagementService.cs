using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Kindergarten.BLL.Helper;
using Kindergarten.BLL.Models;
using Kindergarten.BLL.Models.UserManagementDTO;
using Kindergarten.BLL.Models.UserProfileDTO;
using Kindergarten.DAL.Database;
using Kindergarten.DAL.Extend;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Kindergarten.BLL.Services.SendEmail;
using Kindergarten.BLL.Models.ActivityLogDTO;
using Kindergarten.DAL.Enum;
using System.Security.Claims;
using System.Text.Json;


namespace Kindergarten.BLL.Services.IdentityServices
{
    public class UserManagementService : IUserManagementService
    {
        #region Props
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ApplicationContext _context;
        private readonly IEmailService _emailService; // إنت ممكن تبنيه أو تستخدم موجود
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IActivityLogService _activityLogService;
        #endregion

        #region Ctor
        public UserManagementService(
            UserManager<ApplicationUser> userManager,
            IMapper mapper,
            ApplicationContext context,
            IEmailService emailService,
            IHttpContextAccessor httpContextAccessor,
            IActivityLogService activityLogService)
        {
            _userManager = userManager;
            _mapper = mapper;
            _context = context;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
            _activityLogService = activityLogService;
        }
        #endregion

        #region Actions
        public async Task<PagedResult<ApplicationUserDTO>> GetAllPaginatedAsync(PaginationFilter filter)
        {
            filter.Page = filter.Page < 1 ? 1 : filter.Page;
            filter.PageSize = filter.PageSize < 1 ? 10 : filter.PageSize;

            var searchText = filter.SearchText?.Trim().ToLower();

            var query = _context.Users
                .Where(u => !u.IsDeleted &&
                    (string.IsNullOrEmpty(searchText) ||
                     u.UserName.ToLower().Contains(searchText)) || 
                     u.Email.ToLower().Contains(searchText));

            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                var isDesc = filter.SortDirection?.ToLower() == "desc";
                switch (filter.SortBy.ToLower())
                {
                    case "username":
                        query = isDesc ? query.OrderByDescending(u => u.UserName) : query.OrderBy(u => u.UserName);
                        break;
                    case "email":
                        query = isDesc ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email);
                        break;
                    case "createdon":
                        query = isDesc ? query.OrderByDescending(u => u.CreatedOn) : query.OrderBy(u => u.CreatedOn);
                        break;
                    default:
                        query = query.OrderBy(u => u.UserName);
                        break;
                }
            }
            else
            {
                query = query.OrderBy(u => u.UserName);
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var mapped = _mapper.Map<List<ApplicationUserDTO>>(users);

            return new PagedResult<ApplicationUserDTO>
            {
                Data = mapped,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<ApplicationUserDTO> GetByIdAsync(string userId)
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

            return user == null ? null : _mapper.Map<ApplicationUserDTO>(user);
        }

        public async Task<bool> UpdateAsync(string userId, UpdateApplicationUserDTO dto, string? performedByUserId, string? performedByUserName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.IsDeleted)
                return false;

            // خذ نسخة من البيانات القديمة قبل التعديل (علشان تسجلها في الـ log)
            var oldUserDto = _mapper.Map<ApplicationUserDTO>(user);

            // حدث البيانات
            user.UserName = dto.UserName;
            user.PhoneNumber = dto.PhoneNumber;
            // user.IsAgree = dto.IsAgree;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

            // نسخة بعد التحديث
            var newUserDto = _mapper.Map<ApplicationUserDTO>(user);

            // سجل الـ ActivityLog
            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = nameof(ApplicationUser),
                EntityId = user.Id,
                ActionType = ActivityActionType.Updated,
                SystemComment = $"تم تعديل بيانات المستخدم: {user.UserName}",
                PerformedByUserId = performedByUserId,
                PerformedByUserName = performedByUserName,
                OldValues = JsonSerializer.Serialize(oldUserDto),
                NewValues = JsonSerializer.Serialize(newUserDto)
            });

            return true;
        }


        public async Task<bool> DeleteAsync(string userId, string? performedByUserId, string? performedByUserName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.IsDeleted)
                return false;

            // نسخة البيانات القديمة قبل التعديل (قبل الحذف)
            var oldUserDto = _mapper.Map<ApplicationUserDTO>(user);

            // تعيين الحذف (Soft delete)
            user.IsDeleted = true;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

            // نسخة البيانات الجديدة بعد التعديل (بعد الحذف)
            var newUserDto = _mapper.Map<ApplicationUserDTO>(user);

            // سجل الـ ActivityLog
            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = nameof(ApplicationUser),
                EntityId = user.Id,
                ActionType = ActivityActionType.Deleted,
                SystemComment = $"تم حذف المستخدم: {user.UserName}",
                PerformedByUserId = performedByUserId,
                PerformedByUserName = performedByUserName,
                OldValues = JsonSerializer.Serialize(oldUserDto),
                NewValues = JsonSerializer.Serialize(newUserDto)
            });

            return true;
        }


        public async Task<(string UserId, bool EmailSent)> CreateUserByAdminAsync(CreateUserByAdminDTO dto)
        {
            // 1. تحقق من صلاحية المستخدم الحالي
            var currentUser = _httpContextAccessor.HttpContext?.User;
            if (currentUser == null || (!currentUser.IsInRole("Admin") && !currentUser.IsInRole("Super Admin")))
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

            // بعد await _emailService.SendEmailAsync(...)
            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = nameof(ApplicationUser),
                EntityId = user.Id,
                ActionType = ActivityActionType.Created,
                SystemComment = $"تم إنشاء مستخدم جديد: {user.UserName}",
                PerformedByUserId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier),
                PerformedByUserName = _httpContextAccessor.HttpContext?.User.Identity?.Name,
                NewValues = JsonSerializer.Serialize(_mapper.Map<ApplicationUserDTO>(user))
            });


            return (user.Id, true);
        }

        #endregion


    }

    public interface IUserManagementService
    {
        Task<(string UserId, bool EmailSent)> CreateUserByAdminAsync(CreateUserByAdminDTO dto);
        Task<PagedResult<ApplicationUserDTO>> GetAllPaginatedAsync(PaginationFilter filter);   // Get all users paginated
        Task<ApplicationUserDTO> GetByIdAsync(string userId);                                 // Get user by ID
        Task<bool> UpdateAsync(string userId, UpdateApplicationUserDTO dto, string? performedByUserId, string? performedByUserName);                  // Update user
        Task<bool> DeleteAsync(string userId, string? performedByUserId, string? performedByUserName);                                     // Delete user
    }
}

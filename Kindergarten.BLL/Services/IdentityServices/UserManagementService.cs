using System.Security.Claims;
using System.Text.Json;
using AutoMapper;
using Kindergarten.BLL.Helper;
using Kindergarten.BLL.Models;
using Kindergarten.BLL.Models.ActivityLogDTO;
using Kindergarten.BLL.Models.UserManagementDTO;
using Kindergarten.BLL.Services.SendEmail;
using Kindergarten.DAL.Database;
using Kindergarten.DAL.Enum;
using Kindergarten.DAL.Extend;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


namespace Kindergarten.BLL.Services.IdentityServices
{
    public class UserManagementService : IUserManagementService
    {
        #region Props
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ApplicationContext _context;
        private readonly IEmailService _emailService;
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
                             u.UserName.ToLower().Contains(searchText) ||
                             u.Email.ToLower().Contains(searchText)));

            // Sorting
            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                var isDesc = filter.SortDirection?.ToLower() == "desc";
                query = filter.SortBy.ToLower() switch
                {
                    "username" => isDesc ? query.OrderByDescending(u => u.UserName) : query.OrderBy(u => u.UserName),
                    "email" => isDesc ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                    "createdon" => isDesc ? query.OrderByDescending(u => u.CreatedOn) : query.OrderBy(u => u.CreatedOn),
                    _ => query.OrderBy(u => u.UserName)
                };
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

            if (user == null)
                throw new KeyNotFoundException("User not found.");

            return _mapper.Map<ApplicationUserDTO>(user);
        }


        public async Task UpdateAsync(string userId, UpdateApplicationUserDTO dto, string? performedByUserId, string? performedByUserName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.IsDeleted)
                throw new KeyNotFoundException("User not found or has been deleted.");

            var oldUserDto = _mapper.Map<ApplicationUserDTO>(user);

            user.UserName = dto.UserName;
            user.PhoneNumber = dto.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

            var newUserDto = _mapper.Map<ApplicationUserDTO>(user);

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
        }


        public async Task DeleteAsync(string userId, string? performedByUserId, string? performedByUserName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.IsDeleted)
                throw new KeyNotFoundException("User not found or already deleted.");

            var oldUserDto = _mapper.Map<ApplicationUserDTO>(user);

            user.IsDeleted = true;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

            var newUserDto = _mapper.Map<ApplicationUserDTO>(user);

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
        }


        public async Task<(string UserId, bool EmailSent)> CreateUserByAdminAsync(CreateUserByAdminDTO dto)
        {
            var currentUser = _httpContextAccessor.HttpContext?.User;
            if (currentUser == null || (!currentUser.IsInRole("Admin") && !currentUser.IsInRole("Super Admin")))
                throw new UnauthorizedAccessException("You are not authorized to create users.");

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

            var tempPassword = PasswordGenerator.GenerateSecureTemporaryPassword();

            var result = await _userManager.CreateAsync(user, tempPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");

            if (dto.Roles.Any())
            {
                var addToRolesResult = await _userManager.AddToRolesAsync(user, dto.Roles);
                if (!addToRolesResult.Succeeded)
                    throw new InvalidOperationException($"Failed to assign roles: {string.Join(", ", addToRolesResult.Errors.Select(e => e.Description))}");
            }

            var loginUrl = dto.RedirectUrlAfterResetPassword;

            var emailBody = $@"
                <!DOCTYPE html>
                <html lang=""ar"">
                <head>
                    <meta charset=""UTF-8"">
                    <style>
                        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f7f7f7; color: #333; direction: rtl; padding: 20px; }}
                        .container {{ background-color: #ffffff; border-radius: 8px; padding: 30px; max-width: 600px; margin: auto; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05); }}
                        .title {{ color: #2d89ef; font-size: 24px; margin-bottom: 20px; text-align: center; }}
                        .info {{ font-size: 16px; line-height: 1.8; margin-bottom: 25px; }}
                        .highlight {{ background-color: #f0f0f0; padding: 10px; border-radius: 5px; font-family: monospace; margin-bottom: 20px; }}
                        .btn {{ display: inline-block; background-color: #2d89ef; color: white; padding: 12px 24px; border-radius: 6px; text-decoration: none; font-weight: bold; }}
                        .footer {{ margin-top: 30px; font-size: 14px; color: #888; text-align: center; }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""title"">مرحباً {user.Email} 👋</div>
                        <div class=""info"">تم إنشاء حساب جديد لك في نظام الحضانة. يمكنك تسجيل الدخول باستخدام البيانات التالية:</div>
                        <div class=""highlight"">
                            <div><strong>البريد الإلكتروني:</strong> {user.Email}</div>
                            <div><strong>كلمة المرور المؤقتة:</strong> {tempPassword}</div>
                        </div>
                        <div style=""text-align: center; margin-bottom: 20px;"">
                            <a href=""{loginUrl}"" class=""btn"">تسجيل الدخول الآن</a>
                        </div>
                        <div class=""info"">يرجى تغيير كلمة المرور بعد تسجيل الدخول مباشرة لضمان أمان حسابك.</div>
                        <div class=""footer"">هذا البريد تم إرساله تلقائيًا من نظام إدارة الحضانة.</div>
                    </div>
                </body>
                </html>";

            await _emailService.SendEmailAsync(user.Email, "بيانات الدخول إلى نظام الحضانة", emailBody);

            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = nameof(ApplicationUser),
                EntityId = user.Id,
                ActionType = ActivityActionType.Created,
                SystemComment = $"تم إنشاء مستخدم جديد: {user.UserName}",
                PerformedByUserId = currentUser.FindFirstValue(ClaimTypes.NameIdentifier),
                PerformedByUserName = currentUser.Identity?.Name,
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
        Task UpdateAsync(string userId, UpdateApplicationUserDTO dto, string? performedByUserId, string? performedByUserName);                  // Update user
        Task DeleteAsync(string userId, string? performedByUserId, string? performedByUserName);                                     // Delete user
    }
}

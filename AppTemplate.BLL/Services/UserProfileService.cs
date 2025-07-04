using System.Text.Json;
using AutoMapper;
using AppTemplate.BLL.Helper;
using AppTemplate.BLL.Models;
using AppTemplate.BLL.Models.ActivityLogDTO;
using AppTemplate.BLL.Models.UserProfileDTO;
using AppTemplate.BLL.Services.SendEmail;
using AppTemplate.DAL.Database;
using AppTemplate.DAL.Entity;
using AppTemplate.DAL.Enum;
using AppTemplate.DAL.Extend;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AppTemplate.BLL.Services
{
    public class UserProfileService : IUserProfileService
    {
        #region Prop
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IEmailService _emailService; // إنت ممكن تبنيه أو تستخدم موجود
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMapper _mapper;
        private readonly ApplicationContext _db;
        private readonly IOptions<AdminSettings> _adminSettings;
        private readonly IActivityLogService _activityLogService;
        #endregion

        #region CTOR
        public UserProfileService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IEmailService emailService,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ApplicationContext db,
            IOptions<AdminSettings> adminSettings,
            IActivityLogService activityLogService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
            _mapper = mapper;
            _db = db;
            _adminSettings = adminSettings;
            _activityLogService = activityLogService;
        }
        #endregion

        #region Actions

        public async Task<bool> CompleteBasicProfileAsync(string userId, CompleteBasicProfileDTO dto)
        {
            var existingProfile = await _db.UserBasicProfiles.FindAsync(userId);

            if (existingProfile != null)
            {
                if (existingProfile.Status != UserStatus.rejected)
                {
                    throw new InvalidOperationException("لا يمكن تعديل الملف الشخصي إلا إذا كان مرفوضًا.");
                }

                var oldValues = JsonSerializer.Serialize(existingProfile);

                // تعديل الملف المرفوض
                _mapper.Map(dto, existingProfile);
                existingProfile.Status = UserStatus.pendingApproval;
                existingProfile.SubmittedAt = DateTime.UtcNow;

                _db.UserBasicProfiles.Update(existingProfile);

                var newValues = JsonSerializer.Serialize(existingProfile);

                await _activityLogService.CreateAsync(new ActivityLogCreateDTO
                {
                    EntityName = "UserBasicProfile",
                    EntityId = existingProfile.UserId,
                    ActionType = ActivityActionType.Updated,
                    SystemComment = "تعديل ملف مرفوض وإرساله للمراجعة",
                    PerformedByUserId = userId,
                    OldValues = oldValues,
                    NewValues = newValues,
                });
            }
            else
            {
                var profile = _mapper.Map<UserBasicProfile>(dto);
                profile.UserId = userId;
                profile.Status = UserStatus.pendingApproval;
                profile.SubmittedAt = DateTime.UtcNow;

                await _db.UserBasicProfiles.AddAsync(profile);

                var newValues = JsonSerializer.Serialize(profile);

                await _activityLogService.CreateAsync(new ActivityLogCreateDTO
                {
                    EntityName = "UserBasicProfile",
                    EntityId = profile.UserId,
                    ActionType = ActivityActionType.Created,
                    SystemComment = "إنشاء ملف شخصي جديد وإرساله للمراجعة",
                    PerformedByUserId = userId,
                    NewValues = newValues,
                });
            }

            var result = await _db.SaveChangesAsync();

            if (result <= 0)
            {
                throw new InvalidOperationException("فشل حفظ البيانات. يرجى المحاولة لاحقًا.");
            }

            // إرسال إشعار إلى الإدارة
            var adminEmail = _adminSettings.Value.NotificationEmail;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("المستخدم غير موجود.");

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


        public async Task<UserStatus> GetUserStatusAsync(string userId)
        {
            var status = await _db.UserBasicProfiles
                .Where(p => p.UserId == userId)
                .Select(p => p.Status)
                .FirstOrDefaultAsync();

            if (status == default)
                throw new KeyNotFoundException("الملف الشخصي للمستخدم غير موجود.");

            return status;
        }


        public async Task ReviewUserProfileByAdminAsync(ReviewUserProfileByAdminDTO dto, string? reviewedById, string? reviewedByUserName)
        {
            var profile = await _db.UserBasicProfiles.FirstOrDefaultAsync(p => p.UserId == dto.UserId)
                          ?? throw new KeyNotFoundException("لم يتم العثور على بيانات الموظف.");

            var user = await _userManager.FindByIdAsync(dto.UserId)
                        ?? throw new KeyNotFoundException("المستخدم غير موجود.");

            var oldStatus = profile.Status;
            var oldRejectionReason = profile.RejectionReason;

            profile.Status = dto.IsApproved ? UserStatus.approved : UserStatus.rejected;
            profile.RejectionReason = dto.IsApproved ? null : dto.RejectionReason;
            profile.ReviewedBy = reviewedById;
            profile.ReviewedAt = DateTime.UtcNow;

            _db.Attach(profile);
            var entry = _db.Entry(profile);
            entry.Property(p => p.Status).IsModified = true;
            entry.Property(p => p.RejectionReason).IsModified = true;
            entry.Property(p => p.ReviewedBy).IsModified = true;
            entry.Property(p => p.ReviewedAt).IsModified = true;

            if (dto.IsApproved && !user.IsAgree)
            {
                user.IsAgree = true;

                var updateUserResult = await _userManager.UpdateAsync(user);
                if (!updateUserResult.Succeeded)
                {
                    throw new InvalidOperationException("حدث خطأ أثناء تحديث بيانات المستخدم.");
                }
            }

            await _db.SaveChangesAsync();

            var oldValuesJson = JsonSerializer.Serialize(new
            {
                Status = oldStatus,
                RejectionReason = oldRejectionReason
            });

            var newValuesJson = JsonSerializer.Serialize(new
            {
                Status = profile.Status,
                RejectionReason = profile.RejectionReason
            });

            await _activityLogService.CreateAsync(new ActivityLogCreateDTO
            {
                EntityName = "UserBasicProfile",
                EntityId = profile.UserId,
                ActionType = ActivityActionType.Updated,
                SystemComment = dto.IsApproved ? "تمت الموافقة على ملف المستخدم." : "تم رفض ملف المستخدم.",
                PerformedByUserId = reviewedById,
                PerformedByUserName = reviewedByUserName,
                OldValues = oldValuesJson,
                NewValues = newValuesJson,
                UserComment = dto.RejectionReason
            });

            // إرسال البريد الإلكتروني (الكود الحالي)

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
        }


        // get all Users Profiles 
        public async Task<PagedResult<GetUsersProfilesDTO>> GetAllUsersProfilesForAdminAsync(PaginationFilter filter)
        {
            var query = _db.UserBasicProfiles
                .Include(p => p.User)
                .Where(p => p.IsDeleted == false && p.Status == UserStatus.pendingApproval)
                .AsQueryable();

            // 🔍 Search
            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var searchText = filter.SearchText.Trim().ToLower();
                query = query.Where(p =>
                    p.User.FullName.ToLower().Contains(searchText) ||
                    p.User.Email.ToLower().Contains(searchText) ||
                    p.User.CreatedOn.ToString().ToLower().Contains(searchText));
            }

            // 🔃 Sorting
            if (!string.IsNullOrWhiteSpace(filter.SortBy))
            {
                var isDesc = filter.SortDirection?.ToLower() == "desc";

                switch (filter.SortBy?.Trim().Replace(" ", "").ToLower())
                {
                    case "fullname":
                        query = isDesc
                            ? query.OrderByDescending(p => p.User.UserName)
                            : query.OrderBy(p => p.User.UserName);
                        break;

                    case "primaryphone":
                    case "phonenumber":
                        query = isDesc
                            ? query.OrderByDescending(p => p.User.PhoneNumber)
                            : query.OrderBy(p => p.User.PhoneNumber);
                        break;

                    case "submittedat":
                        query = isDesc
                            ? query.OrderByDescending(p => p.SubmittedAt)
                            : query.OrderBy(p => p.SubmittedAt);
                        break;

                    default:
                        query = query.OrderByDescending(p => p.SubmittedAt); // default sort
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(p => p.SubmittedAt); // fallback sort
            }

            // 📊 Pagination
            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var data = _mapper.Map<List<GetUsersProfilesDTO>>(items);

            return new PagedResult<GetUsersProfilesDTO>
            {
                Data = data,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }



        public async Task<GetUsersProfilesDTO> GetUserProfileByUserIdAsync(string userId)
        {
            var profile = await _db.UserBasicProfiles
                .Include(p => p.User)
                .Where(p => p.IsDeleted == false && p.UserId == userId)
                .FirstOrDefaultAsync();

            if (profile == null)
                throw new KeyNotFoundException("الملف الشخصي للمستخدم غير موجود.");

            return _mapper.Map<GetUsersProfilesDTO>(profile);
        }


        #endregion
    }

    public interface IUserProfileService
    {
        Task<bool> CompleteBasicProfileAsync(string userId, CompleteBasicProfileDTO dto);
        Task<UserStatus> GetUserStatusAsync(string userId);
        Task ReviewUserProfileByAdminAsync(ReviewUserProfileByAdminDTO dto, string? reviewedById, string? reviewedByUserName);
        Task<PagedResult<GetUsersProfilesDTO>> GetAllUsersProfilesForAdminAsync(PaginationFilter filter);
        Task<GetUsersProfilesDTO> GetUserProfileByUserIdAsync(string userId);
    }
}

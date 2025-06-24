using System.Linq;
using System.Net;
using System.Security.Claims;
using AutoMapper;
using Kindergarten.BLL.Helper;
using Kindergarten.BLL.Models;
using Kindergarten.BLL.Models.UsersManagementDTO;
using Kindergarten.BLL.Services;
using Kindergarten.DAL.Database;
using Kindergarten.DAL.Entity;
using Kindergarten.DAL.Enum;
using Kindergarten.DAL.Extend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.API.Controllers
{
    [Authorize(Roles = "Admin,Super Admin")]
    public class UserController : BaseController
    {

        #region Prop
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ApplicationContext db;
        private readonly ICustomUsersService _customUserService;
        private readonly IMapper _mapper;
        #endregion


        #region CTOR
        public UserController(UserManager<ApplicationUser> _userManager,
                                        RoleManager<ApplicationRole> _roleManager,
                                        ApplicationContext db,
                                        ICustomUsersService customUserService,
                                        IMapper mapper)
        {
            this._userManager = _userManager;
            this._roleManager = _roleManager;
            this.db = db;
            this._customUserService = customUserService;
            _mapper = mapper;
        }
        #endregion


        #region Actions
        [HttpGet("GetAllPaginated")]
        public async Task<IActionResult> GetAllPaginated([FromQuery] PaginationFilter filter)
        {
            try
            {
                var query = _userManager.Users.AsQueryable();

                // 🔍 Apply Search
                if (!string.IsNullOrWhiteSpace(filter.SearchText))
                {
                    var search = filter.SearchText.Trim().ToLower();
                    query = query.Where(u =>
                        u.UserName.ToLower().Contains(search) ||
                        u.Email.ToLower().Contains(search)
                    );
                }

                // 🔢 Total count before pagination
                var totalCount = await query.CountAsync();

                // 🔃 Apply Sorting
                if (!string.IsNullOrWhiteSpace(filter.SortBy))
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
                        default:
                            query = query.OrderBy(u => u.UserName);
                            break;
                    }
                }
                else
                {
                    query = query.OrderBy(u => u.UserName); // Default order
                }

                // 📊 Apply Pagination
                var users = await query
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                var result = new PagedResult<UserListDTO>
                {
                    Data = _mapper.Map<List<UserListDTO>>(users),
                    TotalCount = totalCount,
                    Page = filter.Page,
                    PageSize = filter.PageSize
                };

                return Ok(new ApiResponse<PagedResult<UserListDTO>>
                {
                    Code = 200,
                    Status = "Success",
                    Result = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Code = 500,
                    Status = "Error",
                    Result = ex.Message
                });
            }
        }

        // GET /api/user/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!User.IsInRole("Admin") && !User.IsInRole("Super Admin") && currentUserId != id)
                return Forbid();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email
            });
        }

        // PUT /api/user/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] ApplicationUser updatedUser)
        {
            if (id != updatedUser.Id)
                return BadRequest("User ID mismatch");

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.UserName = updatedUser.UserName;
            user.Email = updatedUser.Email;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(updatedUser);
        }

        // DELETE /api/user/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = $"User with id '{id}' deleted successfully." });
        }

        // GET /api/roles
        [HttpGet("allroles")]
        public IActionResult GetAllRoles()
        {
            var roles = _roleManager.Roles.Select(r => r.Name).ToList();
            return Ok(roles);
        }

        // GET /api/user/{id}/roles
        [HttpGet("{id}/roles")]
        public async Task<IActionResult> GetUserRoles(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(roles);
        }

        // POST /api/user/{id}/roles
        [HttpPost("{id}/roles")]
        public async Task<IActionResult> UpdateUserRoles(string id, [FromBody] List<string> roles)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var validRoles = _roleManager.Roles.Select(r => r.Name).ToList();
            if (roles.Except(validRoles).Any())
                return BadRequest("Invalid roles specified");

            var currentRoles = await _userManager.GetRolesAsync(user);
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
                return BadRequest(removeResult.Errors);

            var addResult = await _userManager.AddToRolesAsync(user, roles);
            if (!addResult.Succeeded)
                return BadRequest(addResult.Errors);

            // إرجاع الأدوار المحدثة
            var updatedRoles = await _userManager.GetRolesAsync(user);
            return Ok(updatedRoles);
        }


        // GET /api/claims
        [HttpGet("allclaims")]
        public IActionResult GetAllClaims()
        {
            var claims = ClaimsStore.AllClaims
                .Select(c => new { c.Type, c.Value })
                .ToList();

            return Ok(claims);
        }

        // GET /api/user/{id}/claims
        [HttpGet("{id}/claims")]
        public async Task<IActionResult> GetUserClaims(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var claims = await _userManager.GetClaimsAsync(user);
            var claimTypes = claims.Select(c => c.Type).ToList();

            return Ok(claimTypes);
        }

        // POST /api/user/{id}/claims
        [HttpPost("{id}/claims")]
        public async Task<IActionResult> UpdateUserClaims(string id, [FromBody] List<string> claims)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // تحقق من صلاحية الـ claims (مثلاً: ClaimsStore.AllClaims)
            var validClaims = ClaimsStore.AllClaims.Select(c => c.Type).ToList();
            if (claims.Except(validClaims).Any())
                return BadRequest("Invalid claims specified");

            var currentClaims = await _userManager.GetClaimsAsync(user);
            foreach (var claim in currentClaims)
            {
                var result = await _userManager.RemoveClaimAsync(user, claim);
                if (!result.Succeeded)
                    return BadRequest(result.Errors);
            }

            foreach (var claimType in claims)
            {
                var result = await _userManager.AddClaimAsync(user, new Claim(claimType, "true"));
                if (!result.Succeeded)
                    return BadRequest(result.Errors);
            }

            // إرجاع الـ claims المحدثة
            var updatedClaims = await _userManager.GetClaimsAsync(user);
            var claimTypes = updatedClaims.Select(c => c.Type).ToList();

            return Ok(claimTypes);
        }

        [HttpPost("create-by-admin")]
        public async Task<IActionResult> CreateUserByAdmin([FromBody] CreateUserByAdminDTO dto)
        {
            try
            {
                // 1. التحقق من صحة البيانات
                if (dto == null)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Code = (int)HttpStatusCode.BadRequest,
                        Status = "Error",
                        Result = "Invalid data."
                    });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                                           .SelectMany(v => v.Errors)
                                           .Select(e => e.ErrorMessage)
                                           .ToList();

                    return BadRequest(new ApiResponse<object>
                    {
                        Code = (int)HttpStatusCode.BadRequest,
                        Status = "Validation Error",
                        Result = errors
                    });
                }

                // 2. تنفيذ العملية
                var result = await _customUserService.CreateUserByAdminAsync(dto);

                // 3. إرجاع النتيجة بنجاح
                return Ok(new ApiResponse<object>
                {
                    Code = (int)HttpStatusCode.OK,
                    Status = "Success",
                    Result = new
                    {
                        Message = "User created successfully.",
                        UserId = result.UserId,
                        EmailSent = result.EmailSent
                    }
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode((int)HttpStatusCode.Forbidden, new ApiResponse<string>
                {
                    Code = (int)HttpStatusCode.Forbidden,
                    Status = "Forbidden",
                    Result = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new ApiResponse<object>
                {
                    Code = (int)HttpStatusCode.InternalServerError,
                    Status = "Error",
                    Result = ex.Message
                });
            }
        }

        #endregion

    }
}

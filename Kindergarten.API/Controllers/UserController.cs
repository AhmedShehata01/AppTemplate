using System.Net;
using System.Security.Claims;
using Kindergarten.BLL.Helper;
using Kindergarten.BLL.Models.UsersManagementDTO;
using Kindergarten.BLL.Services;
using Kindergarten.DAL.Database;
using Kindergarten.DAL.Enum;
using Kindergarten.DAL.Extend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Kindergarten.API.Controllers
{
    public class UserController : BaseController
    {

        #region Prop
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ApplicationContext db;
        private readonly ICustomUsersService _customUserService;
        #endregion


        #region CTOR
        public UserController(UserManager<ApplicationUser> _userManager,
                                        RoleManager<ApplicationRole> _roleManager,
                                        ApplicationContext db,
                                        ICustomUsersService customUserService)
        {
            this._userManager = _userManager;
            this._roleManager = _roleManager;
            this.db = db;
            this._customUserService = customUserService;
        }
        #endregion


        #region Actions
        // GET /api/user
        [HttpGet]
        [Authorize(Roles = "Admin,Super Admin")]
        public IActionResult GetAllUsers()
        {
            var users = _userManager.Users.Select(u => new
            {
                u.Id,
                u.UserName,
                u.Email
            }).ToList();

            return Ok(users);
        }


        // GET /api/user/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Super Admin")]
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
        [Authorize(Roles = "Admin,Super Admin")]
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
        [Authorize(Roles = "Super Admin")]
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
        [Authorize(Roles = "Admin,Super Admin")]
        public IActionResult GetAllRoles()
        {
            var roles = _roleManager.Roles.Select(r => r.Name).ToList();
            return Ok(roles);
        }

        // GET /api/user/{id}/roles
        [HttpGet("{id}/roles")]
        [Authorize(Roles = "Admin,Super Admin")]
        public async Task<IActionResult> GetUserRoles(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(roles);
        }

        // POST /api/user/{id}/roles
        [HttpPost("{id}/roles")]
        [Authorize(Roles = "Admin,Super Admin")]
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
        [Authorize(Roles = "Admin,Super Admin")]
        public IActionResult GetAllClaims()
        {
            var claims = ClaimsStore.AllClaims
                .Select(c => new { c.Type, c.Value })
                .ToList();

            return Ok(claims);
        }

        // GET /api/user/{id}/claims
        [HttpGet("{id}/claims")]
        [Authorize(Roles = "Admin,Super Admin")]
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
        [Authorize(Roles = "Admin,Super Admin")]
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
        [Authorize(Roles = "Admin,Super Admin")]
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

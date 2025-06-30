using System.Net;
using System.Security.Claims;
using Kindergarten.BLL.Helper;
using Kindergarten.BLL.Models;
using Kindergarten.BLL.Models.ClaimsDTO;
using Kindergarten.BLL.Models.UserManagementDTO;
using Kindergarten.BLL.Services.IdentityServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kindergarten.API.Controllers
{
    [Authorize(Roles = "Admin,Super Admin")]
    public class UserController : BaseController
    {
        #region Services
        private readonly IUserManagementService _userManagementService;
        private readonly IUserRoleManagementService _userRoleManagementService;
        private readonly IUserClaimManagementService _userClaimManagementService;
        #endregion

        #region CTOR
        public UserController(
            IUserManagementService userManagementService,
            IUserRoleManagementService userRoleManagementService,
            IUserClaimManagementService userClaimManagementService)
        {
            _userManagementService = userManagementService;
            _userRoleManagementService = userRoleManagementService;
            _userClaimManagementService = userClaimManagementService;
        }
        #endregion

        #region Actions

        [HttpGet("GetAllPaginated")]
        public async Task<IActionResult> GetAllPaginated([FromQuery] PaginationFilter filter)
        {
            try
            {
                var result = await _userManagementService.GetAllPaginatedAsync(filter);
                return Ok(new ApiResponse<PagedResult<ApplicationUserDTO>>
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


        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!User.IsInRole("Admin") && !User.IsInRole("Super Admin") && currentUserId != id)
                {
                    return StatusCode((int)HttpStatusCode.Forbidden, new ApiResponse<string>
                    {
                        Code = 403,
                        Status = "Forbidden",
                        Result = "You are not authorized to view this user."
                    });
                }

                var user = await _userManagementService.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Code = 404,
                        Status = "NotFound",
                        Result = "User not found."
                    });
                }

                return Ok(new ApiResponse<ApplicationUserDTO>
                {
                    Code = 200,
                    Status = "Success",
                    Result = user
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


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateApplicationUserDTO updatedUser)
        {
            try
            {
                if (id != updatedUser.Id)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Code = 400,
                        Status = "Error",
                        Result = "User ID mismatch"
                    });
                }

                var success = await _userManagementService.UpdateAsync(id, updatedUser, CurrentUserId , CurrentUserName);
                if (!success)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Code = 404,
                        Status = "NotFound",
                        Result = "User not found."
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Code = 200,
                    Status = "Updated",
                    Result = true
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


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var success = await _userManagementService.DeleteAsync(id, CurrentUserId ,CurrentUserName);
                if (!success)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Code = 404,
                        Status = "NotFound",
                        Result = "User not found."
                    });
                }

                return Ok(new ApiResponse<string>
                {
                    Code = 200,
                    Status = "Deleted",
                    Result = $"User with id '{id}' deleted successfully."
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


        [HttpGet("allroles")]
        public async Task<IActionResult> GetAllRoles()
        {
            try
            {
                var roles = await _userRoleManagementService.GetAllRolesAsync();
                return Ok(new ApiResponse<List<string>>
                {
                    Code = 200,
                    Status = "Success",
                    Result = roles
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


        [HttpGet("roles/{id}")]
        public async Task<IActionResult> GetUserRoles(string id)
        {
            try
            {
                var roles = await _userRoleManagementService.GetUserRolesAsync(id);
                if (roles == null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Code = 404,
                        Status = "NotFound",
                        Result = "User not found."
                    });
                }

                return Ok(new ApiResponse<List<string>>
                {
                    Code = 200,
                    Status = "Success",
                    Result = roles
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


        [HttpPost("roles/{id}")]
        public async Task<IActionResult> UpdateUserRoles(string id, [FromBody] List<string> roles)
        {
            try
            {
                var result = await _userRoleManagementService.AssignRolesToUserAsync(id, roles, CurrentUserId , CurrentUserName);
                if (!result)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Code = 400,
                        Status = "Error",
                        Result = "Failed to update roles."
                    });
                }

                var updatedRoles = await _userRoleManagementService.GetUserRolesAsync(id);
                return Ok(new ApiResponse<List<string>>
                {
                    Code = 200,
                    Status = "Updated",
                    Result = updatedRoles
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


        [HttpGet("allclaims")]
        public async Task<IActionResult> GetAllClaims()
        {
            try
            {
                var claims = await _userClaimManagementService.GetAllClaimsAsync();
                return Ok(new ApiResponse<List<ClaimDTO>>
                {
                    Code = 200,
                    Status = "Success",
                    Result = claims
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


        [HttpGet("claims/{id}")]
        public async Task<IActionResult> GetUserClaims(string id)
        {
            try
            {
                var result = await _userClaimManagementService.GetUserClaimsAsync(id);
                if (result == null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Code = 404,
                        Status = "NotFound",
                        Result = "User not found."
                    });
                }

                return Ok(new ApiResponse<List<ClaimDTO>>
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


        [HttpPost("claims/{id}")]
        public async Task<IActionResult> UpdateUserClaims(string id, [FromBody] List<string> claims)
        {
            try
            {
                var success = await _userClaimManagementService.AssignClaimsToUserAsync(id, claims, CurrentUserId , CurrentUserName);
                if (!success)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Code = 400,
                        Status = "Error",
                        Result = "Failed to update claims."
                    });
                }

                var updatedClaims = await _userClaimManagementService.GetUserClaimsAsync(id);
                return Ok(new ApiResponse<List<ClaimDTO>>
                {
                    Code = 200,
                    Status = "Updated",
                    Result = updatedClaims
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


        [HttpPost("create-by-admin")]
        public async Task<IActionResult> CreateUserByAdmin([FromBody] CreateUserByAdminDTO dto)
        {
            try
            {
                if (dto == null || !ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponse<object>
                    {
                        Code = 400,
                        Status = "Validation Error",
                        Result = errors
                    });
                }

                var result = await _userManagementService.CreateUserByAdminAsync(dto);

                return Ok(new ApiResponse<object>
                {
                    Code = 200,
                    Status = "Success",
                    Result = new
                    {
                        Message = "User created successfully.",
                        result.UserId,
                        result.EmailSent
                    }
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

        #endregion
    }
}

using Kindergarten.BLL.Helper;
using Kindergarten.BLL.Models.RoleManagementDTO;
using Kindergarten.BLL.Models;
using Kindergarten.BLL.Services.IdentityServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Kindergarten.BLL.Models.UserManagementDTO;

namespace Kindergarten.API.Controllers
{
    [Authorize(Roles = "Super Admin, Admin")]
    public class RoleManagementController : BaseController
    {
        #region Prop
        private readonly IRoleManagementService _roleService;
        #endregion

        #region Ctor
        public RoleManagementController(IRoleManagementService roleService)
        {
            _roleService = roleService;
        }
        #endregion

        #region Actions
        [HttpGet("getAllPaginated")]
        public async Task<IActionResult> GetAllPaginated([FromQuery] PaginationFilter filter)
        {
            try
            {
                var result = await _roleService.GetAllPaginatedAsync(filter);
                return Ok(new ApiResponse<PagedResult<ApplicationRoleDTO>>
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


        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var role = await _roleService.GetByIdAsync(id);
                if (role == null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Code = 404,
                        Status = "Not Found",
                        Result = "Role not found."
                    });
                }

                return Ok(new ApiResponse<ApplicationRoleDTO>
                {
                    Code = 200,
                    Status = "Success",
                    Result = role
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


        [HttpPost("create")]
        public async Task<IActionResult> Create(CreateRoleDTO dto)
        {
            try
            {
                var created = await _roleService.CreateRoleAsync(dto , CurrentUserId , CurrentUserName);
                return Ok(new ApiResponse<bool>
                {
                    Code = 200,
                    Status = "Success",
                    Result = created
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


        [HttpPut("update/{id}")]
        public async Task<IActionResult> Update(string id, UpdateRoleDTO dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Code = 400,
                    Status = "Invalid",
                    Result = "Mismatched role ID."
                });
            }

            try
            {
                var updated = await _roleService.UpdateRoleAsync(dto, CurrentUserId, CurrentUserName);
                if (!updated)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Code = 404,
                        Status = "Not Found",
                        Result = "Role not found."
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Code = 200,
                    Status = "Success",
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


        [HttpPut("toggle-status/{id}")]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            try
            {
                var toggled = await _roleService.ToggleRoleStatusAsync(id , CurrentUserId , CurrentUserName);
                if (!toggled)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Code = 404,
                        Status = "Not Found",
                        Result = "Role not found or deleted."
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Code = 200,
                    Status = "Success",
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


        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var deleted = await _roleService.DeleteRoleAsync(id, CurrentUserId , CurrentUserName);
                if (!deleted)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Code = 404,
                        Status = "Not Found",
                        Result = "Role not found."
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Code = 200,
                    Status = "Success",
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



        [HttpGet("roles-with-routes")]
        public async Task<IActionResult> GetRolesWithRoutes()
        {
            try
            {
                var roles = await _roleService.GetRolesWithRoutesAsync();
                return Ok(new ApiResponse<List<RoleWithRoutesDTO>>
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


        [HttpGet("roles-dropdown")]
        public async Task<IActionResult> GetDropdownRoles()
        {
            try
            {
                var roles = await _roleService.GetDropdownRolesAsync();
                return Ok(new ApiResponse<List<DropdownRoleDTO>>
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

        /// <summary>
        /// Get all users assigned to a specific role
        /// </summary>
        [HttpGet("{roleId}/users")]
        public async Task<IActionResult> GetUsersByRole(string roleId)
        {
            try
            {
                var users = await _roleService.GetUsersByRoleAsync(roleId);
                return Ok(new ApiResponse<List<ApplicationUserDTO>>
                {
                    Code = 200,
                    Status = "Success",
                    Result = users
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Code = 500,
                    Status = "Error",
                    Result = "An error occurred while retrieving users."
                });
            }
        }

        /// <summary>
        /// Remove a user from a specific role
        /// </summary>
        [HttpDelete("{roleId}/users/{userId}")]
        public async Task<IActionResult> RemoveUserFromRole(string roleId, string userId)
        {
            try
            {
                var removed = await _roleService.RemoveUserRoleAsync(userId, roleId , CurrentUserId , CurrentUserName);
                if (!removed)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Code = 404,
                        Status = "Not Found",
                        Result = "User or role assignment not found."
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Code = 200,
                    Status = "Success",
                    Result = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Code = 500,
                    Status = "Error",
                    Result = $"An error occurred while removing user from role , {ex}."
                });
            }
        }
        #endregion
    }
}

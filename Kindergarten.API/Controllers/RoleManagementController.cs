using Kindergarten.BLL.Helper;
using Kindergarten.BLL.Models;
using Kindergarten.BLL.Models.RoleManagementDTO;
using Kindergarten.BLL.Models.UserManagementDTO;
using Kindergarten.BLL.Services.IdentityServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            var result = await _roleService.GetAllPaginatedAsync(filter);

            return Ok(new ApiResponse<PagedResult<ApplicationRoleDTO>>
            {
                Code = 200,
                Status = "Success",
                Result = result
            });
        }



        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var role = await _roleService.GetByIdAsync(id);

            return Ok(new ApiResponse<ApplicationRoleDTO>
            {
                Code = 200,
                Status = "Success",
                Result = role
            });
        }



        [HttpPost("create")]
        public async Task<IActionResult> Create(CreateRoleDTO dto)
        {
            var roleId = await _roleService.CreateRoleAsync(dto, CurrentUserId, CurrentUserName);

            return Ok(new ApiResponse<string>
            {
                Code = 200,
                Status = "Created",
                Result = roleId
            });
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

            await _roleService.UpdateRoleAsync(dto, CurrentUserId, CurrentUserName);

            return Ok(new ApiResponse<bool>
            {
                Code = 200,
                Status = "Updated",
                Result = true
            });
        }



        [HttpPut("toggle-status/{id}")]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            await _roleService.ToggleRoleStatusAsync(id, CurrentUserId, CurrentUserName);

            return Ok(new ApiResponse<bool>
            {
                Code = 200,
                Status = "Toggled",
                Result = true
            });
        }



        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _roleService.DeleteRoleAsync(id, CurrentUserId, CurrentUserName);

            return Ok(new ApiResponse<bool>
            {
                Code = 200,
                Status = "Success",
                Result = true
            });
        }



        [HttpGet("roles-with-routes")]
        public async Task<IActionResult> GetRolesWithRoutes()
        {
            var roles = await _roleService.GetRolesWithRoutesAsync();
            return Ok(new ApiResponse<List<RoleWithRoutesDTO>>
            {
                Code = 200,
                Status = "Success",
                Result = roles
            });

        }


        [HttpGet("roles-dropdown")]
        public async Task<IActionResult> GetDropdownRoles()
        {
            var roles = await _roleService.GetDropdownRolesAsync();
            return Ok(new ApiResponse<List<DropdownRoleDTO>>
            {
                Code = 200,
                Status = "Success",
                Result = roles
            });
        }

        /// <summary>
        /// Get all users assigned to a specific role
        /// </summary>
        [HttpGet("{roleId}/users")]
        public async Task<IActionResult> GetUsersByRole(string roleId)
        {
            var users = await _roleService.GetUsersByRoleAsync(roleId);
            return Ok(new ApiResponse<List<ApplicationUserDTO>>
            {
                Code = 200,
                Status = "Success",
                Result = users
            });

        }

        /// <summary>
        /// Remove a user from a specific role
        /// </summary>
        [HttpDelete("{roleId}/users/{userId}")]
        public async Task<IActionResult> RemoveUserFromRole(string roleId, string userId)
        {
            await _roleService.RemoveUserRoleAsync(userId, roleId, CurrentUserId, CurrentUserName);
            return Ok(new ApiResponse<bool>
            {
                Code = 200,
                Status = "Success",
                Result = true
            });
        }
        #endregion
    }
}

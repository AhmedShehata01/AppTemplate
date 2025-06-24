using Kindergarten.BLL.Helper;
using Kindergarten.BLL.Models;
using Kindergarten.BLL.Models.RoleManagementDTO;
using Kindergarten.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kindergarten.API.Controllers
{
    [Authorize(Roles = "Super Admin,Admin")]
    public class RoleManagementController : BaseController
    {
        #region Props
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
                var result = await _roleService.GetAllRolesAsync(filter);
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
        public async Task<ActionResult<ApiResponse<ApplicationRoleDTO>>> GetById(string id)
        {
            var result = await _roleService.GetRoleByIdAsync(id);
            if (result == null)
                return NotFound(new ApiResponse<string> { Code = 404, Status = "NotFound", Result = "Role not found" });

            return Ok(new ApiResponse<ApplicationRoleDTO>
            {
                Code = 200,
                Status = "Success",
                Result = result
            });
        }

        [HttpPost("create")]
        public async Task<ActionResult<ApiResponse<string>>> Create([FromBody] CreateRoleDTO dto)
        {
            try
            {
                var roleId = await _roleService.CreateRoleAsync(dto);
                return Ok(new ApiResponse<string>
                {
                    Code = 201,
                    Status = "Created",
                    Result = roleId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Code = 400,
                    Status = "Error",
                    Result = ex.Message
                });
            }
        }

        [HttpPut("update")]
        public async Task<ActionResult<ApiResponse<bool>>> Update([FromBody] UpdateRoleDTO dto)
        {
            try
            {
                var updated = await _roleService.UpdateRoleAsync(dto);
                if (!updated)
                    return NotFound(new ApiResponse<string> { Code = 404, Status = "NotFound", Result = "Role not found" });

                return Ok(new ApiResponse<bool>
                {
                    Code = 200,
                    Status = "Updated",
                    Result = true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Code = 400,
                    Status = "Error",
                    Result = ex.Message
                });
            }
        }

        [HttpPut("toggle-status")]
        public async Task<ActionResult<ApiResponse<bool>>> ToggleStatus([FromBody] ToggleRoleStatusDTO dto)
        {
            var toggled = await _roleService.ToggleRoleStatusAsync(dto);
            if (!toggled)
                return NotFound(new ApiResponse<string>
                {
                    Code = 404,
                    Status = "NotFound",
                    Result = "Role not found"
                });

            return Ok(new ApiResponse<bool>
            {
                Code = 200,
                Status = "StatusChanged",
                Result = true
            });
        }

        [HttpGet("roles-with-routes")]
        public async Task<ActionResult<ApiResponse<List<RoleWithRoutesDTO>>>> GetRolesWithRoutes()
        {
            var result = await _roleService.GetRolesWithRoutesAsync();
            return Ok(new ApiResponse<List<RoleWithRoutesDTO>>
            {
                Code = 200,
                Status = "Success",
                Result = result
            });
        }

        [HttpDelete("delete/{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(string id)
        {
            var deleted = await _roleService.DeleteRoleAsync(id);
            if (!deleted)
                return NotFound(new ApiResponse<string>
                {
                    Code = 404,
                    Status = "NotFound",
                    Result = "Role not found or already deleted"
                });

            return Ok(new ApiResponse<bool>
            {
                Code = 200,
                Status = "Deleted",
                Result = true
            });
        }

        #endregion
    }
}

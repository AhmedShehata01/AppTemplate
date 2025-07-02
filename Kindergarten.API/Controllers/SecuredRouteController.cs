using System.Security.Claims;
using Kindergarten.BLL.Helper;
using Kindergarten.BLL.Models;
using Kindergarten.BLL.Models.DRBRADTO;
using Kindergarten.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kindergarten.API.Controllers
{
    [Authorize(Roles = "Super Admin,Admin")]
    public class SecuredRouteController : BaseController
    {
        #region Prop
        private readonly ISecuredRouteService _securedRouteService;
        #endregion

        #region CTOR
        public SecuredRouteController(ISecuredRouteService securedRouteService)
        {
            _securedRouteService = securedRouteService;
        }
        #endregion

        #region Actions 

        [HttpGet("GetAllPaginated")]
        public async Task<IActionResult> GetAllPaginated([FromQuery] PaginationFilter filter)
        {
            try
            {
                var pagedResult = await _securedRouteService.GetAllRoutesAsync(filter);

                return Ok(new ApiResponse<PagedResult<SecuredRouteDTO>>
                {
                    Code = 200,
                    Status = "Success",
                    Result = pagedResult
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
        public async Task<ActionResult<ApiResponse<object>>> GetById(int id)
        {
            try
            {
                var result = await _securedRouteService.GetRouteByIdAsync(id);
                if (result == null)
                {
                    return Ok(new ApiResponse<string>
                    {
                        Code = 404,
                        Status = "NotFound",
                        Result = "Route not found"
                    });
                }

                return Ok(new ApiResponse<SecuredRouteDTO>
                {
                    Code = 200,
                    Status = "Success",
                    Result = result
                });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse<string>
                {
                    Code = 500,
                    Status = "Error",
                    Result = ex.Message
                });
            }
        }

        [HttpPost("create")]
        public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateSecuredRouteDTO dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<string>
                    {
                        Code = 401,
                        Status = "Unauthorized",
                        Result = "User ID not found in token"
                    });
                }

                var id = await _securedRouteService.CreateRouteAsync(dto, CurrentUserId, CurrentUserName);

                return Ok(new ApiResponse<int>
                {
                    Code = 201,
                    Status = "Created",
                    Result = id
                });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse<string>
                {
                    Code = 400,
                    Status = "Error",
                    Result = ex.Message
                });
            }
        }

        [HttpPut("update")]
        public async Task<ActionResult<ApiResponse<object>>> Update([FromBody] UpdateSecuredRouteDTO dto)
        {
            try
            {
                var updated = await _securedRouteService.UpdateRouteAsync(dto, CurrentUserId, CurrentUserName);
                if (!updated)
                {
                    return Ok(new ApiResponse<string>
                    {
                        Code = 404,
                        Status = "NotFound",
                        Result = "Route not found"
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
                return Ok(new ApiResponse<string>
                {
                    Code = 400,
                    Status = "Error",
                    Result = ex.Message
                });
            }
        }

        [HttpDelete("delete/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
        {
            try
            {
                var deleted = await _securedRouteService.DeleteRouteAsync(id, CurrentUserId, CurrentUserName);
                if (!deleted)
                {
                    return Ok(new ApiResponse<string>
                    {
                        Code = 404,
                        Status = "NotFound",
                        Result = "Route not found or already deleted"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Code = 200,
                    Status = "Deleted",
                    Result = true
                });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse<string>
                {
                    Code = 500,
                    Status = "Error",
                    Result = ex.Message
                });
            }
        }

        [HttpPost("assign-roles")]
        public async Task<ActionResult<ApiResponse<object>>> AssignRoles([FromBody] AssignRolesToRouteDTO dto)
        {
            try
            {
                var result = await _securedRouteService.AssignRolesAsync(dto, CurrentUserId, CurrentUserName);
                if (!result)
                {
                    return Ok(new ApiResponse<string>
                    {
                        Code = 400,
                        Status = "Failed",
                        Result = "Failed to assign roles"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Code = 200,
                    Status = "RolesAssigned",
                    Result = true
                });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse<string>
                {
                    Code = 500,
                    Status = "Error",
                    Result = ex.Message
                });
            }
        }

        [HttpPost("unassign-role")]
        public async Task<ActionResult<ApiResponse<object>>> UnassignRole([FromBody] UnassignRoleFromRouteDTO dto)
        {
            try
            {
                var result = await _securedRouteService.UnassignRoleAsync(dto, CurrentUserId, CurrentUserName);
                if (!result)
                {
                    return Ok(new ApiResponse<string>
                    {
                        Code = 400,
                        Status = "Failed",
                        Result = "Role not assigned to this route"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Code = 200,
                    Status = "RoleUnassigned",
                    Result = true
                });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse<string>
                {
                    Code = 500,
                    Status = "Error",
                    Result = ex.Message
                });
            }
        }

        [HttpGet("routes-with-roles")]
        public async Task<ActionResult<ApiResponse<object>>> GetRoutesWithRoles()
        {
            try
            {
                var result = await _securedRouteService.GetRoutesWithRolesAsync();
                return Ok(new ApiResponse<List<RouteWithRolesDTO>>
                {
                    Code = 200,
                    Status = "Success",
                    Result = result
                });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse<string>
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

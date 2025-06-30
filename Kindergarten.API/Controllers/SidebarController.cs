using Kindergarten.BLL.Helper;
using Kindergarten.BLL.Models;
using Kindergarten.BLL.Models.DRBRADTO;
using Kindergarten.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kindergarten.API.Controllers
{
    [Authorize]
    public class SidebarController : BaseController
    {
        #region Prop
        private readonly ISidebarService _sidebarService;
        #endregion

        #region CTOR
        public SidebarController(ISidebarService sidebarService)
        {
            _sidebarService = sidebarService;
        }
        #endregion

        #region Actions 
        [HttpGet("GetAllPaginated")]
        public async Task<IActionResult> GetAllPaginated([FromQuery] PaginationFilter filter)
        {
            try
            {
                var result = await _sidebarService.GetPagedAsync(filter);
                return Ok(new ApiResponse<PagedResult<SidebarItemDTO>>
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

        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var item = await _sidebarService.GetByIdAsync(id);
                if (item == null)
                {
                    return Ok(new ApiResponse<string>
                    {
                        Code = 404,
                        Status = "NotFound",
                        Result = "Sidebar item not found"
                    });
                }

                return Ok(new ApiResponse<SidebarItemDTO>
                {
                    Code = 200,
                    Status = "Success",
                    Result = item
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

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] CreateSidebarItemDTO dto)
        {
            try
            {
                var id = await _sidebarService.CreateAsync(dto, CurrentUserId, CurrentUserName);
                return Ok(new ApiResponse<int>
                {
                    Code = 201,
                    Status = "Created",
                    Result = id
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

        [HttpPut("Update")]
        public async Task<IActionResult> Update([FromBody] UpdateSidebarItemDTO dto)
        {
            try
            {
                var result = await _sidebarService.UpdateAsync(dto, CurrentUserId , CurrentUserName);
                if (!result)
                {
                    return Ok(new ApiResponse<string>
                    {
                        Code = 404,
                        Status = "NotFound",
                        Result = "Sidebar item not found"
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
                return BadRequest(new ApiResponse<string>
                {
                    Code = 400,
                    Status = "Error",
                    Result = ex.Message
                });
            }
        }

        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _sidebarService.SoftDeleteAsync(id , CurrentUserId , CurrentUserName);
                if (!result)
                {
                    return Ok(new ApiResponse<string>
                    {
                        Code = 404,
                        Status = "NotFound",
                        Result = "Sidebar item not found or already deleted"
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
                return StatusCode(500, new ApiResponse<string>
                {
                    Code = 500,
                    Status = "Error",
                    Result = ex.Message
                });
            }
        }

        [HttpGet("parents")]
        public async Task<IActionResult> GetParentItems()
        {
            try
            {
                var items = await _sidebarService.GetParentItemsAsync();
                return Ok(new ApiResponse<List<SidebarItemDTO>>
                {
                    Code = 200,
                    Status = "Success",
                    Result = items
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

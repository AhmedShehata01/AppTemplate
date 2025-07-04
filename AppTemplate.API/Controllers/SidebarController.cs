using AppTemplate.BLL.Helper;
using AppTemplate.BLL.Models;
using AppTemplate.BLL.Models.DRBRADTO;
using AppTemplate.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTemplate.API.Controllers
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
            var result = await _sidebarService.GetPagedAsync(filter);
            return Ok(new ApiResponse<PagedResult<SidebarItemDTO>>
            {
                Code = 200,
                Status = "Success",
                Result = result
            });
        }


        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _sidebarService.GetByIdAsync(id);

            return Ok(new ApiResponse<SidebarItemDTO>
            {
                Code = 200,
                Status = "Success",
                Result = item
            });
        }


        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] CreateSidebarItemDTO dto)
        {
            var id = await _sidebarService.CreateAsync(dto, CurrentUserId, CurrentUserName);

            return StatusCode(201, new ApiResponse<int>
            {
                Code = 201,
                Status = "Created",
                Result = id
            });
        }


        [HttpPut("Update")]
        public async Task<IActionResult> Update([FromBody] UpdateSidebarItemDTO dto)
        {
            await _sidebarService.UpdateAsync(dto, CurrentUserId, CurrentUserName);

            return Ok(new ApiResponse<bool>
            {
                Code = 200,
                Status = "Updated",
                Result = true
            });
        }


        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _sidebarService.SoftDeleteAsync(id, CurrentUserId, CurrentUserName);

            return Ok(new ApiResponse<bool>
            {
                Code = 200,
                Status = "Deleted",
                Result = true
            });
        }


        [HttpGet("parents")]
        public async Task<IActionResult> GetParentItems()
        {
            var items = await _sidebarService.GetParentItemsAsync();
            return Ok(new ApiResponse<List<SidebarItemDTO>>
            {
                Code = 200,
                Status = "Success",
                Result = items
            });
        }
        #endregion

    }
}

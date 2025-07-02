using Kindergarten.BLL.Helper;
using Kindergarten.BLL.Models;
using Kindergarten.BLL.Models.BranchDTO;
using Kindergarten.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kindergarten.API.Controllers
{
    [Authorize]
    public class BranchController : BaseController
    {
        #region Prop
        private readonly IBranchService _branchService;
        #endregion

        #region CTOR
        public BranchController(IBranchService branchService)
        {
            _branchService = branchService;
        }
        #endregion

        #region Actions
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllBranches([FromQuery] PaginationFilter filter)
        {
            var result = await _branchService.GetAllBranchesAsync(filter);

            return Ok(new ApiResponse<PagedResult<BranchDTO>>
            {
                Code = 200,
                Status = "Success",
                Result = result
            });
        }

        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetBranchById(int id)
        {
            var branch = await _branchService.GetBranchByIdAsync(id);

            return Ok(new ApiResponse<BranchDTO>
            {
                Code = 200,
                Status = "Success",
                Result = branch
            });
        }

        [HttpPost("Create")]
        public async Task<IActionResult> CreateBranch([FromBody] BranchCreateDTO branchCreateDto)
        {
            if (!ModelState.IsValid)
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

            var createdBy = User.Identity?.Name ?? "Unknown";

            var createdBranch = await _branchService.CreateBranchAsync(branchCreateDto, createdBy);

            var response = new ApiResponse<BranchDTO>
            {
                Code = 201,
                Status = "Success",
                Result = createdBranch
            };

            return CreatedAtAction(nameof(GetBranchById), new { id = createdBranch.Id }, response);
        }

        [HttpPut("Update")]
        public async Task<IActionResult> UpdateBranch([FromBody] BranchUpdateDTO branchUpdateDto)
        {
            if (!ModelState.IsValid)
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

            var updatedBranch = await _branchService.UpdateBranchAsync(branchUpdateDto, CurrentUserName);

            return Ok(new ApiResponse<BranchDTO>
            {
                Code = 200,
                Status = "Success",
                Result = updatedBranch
            });
        }

        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> DeleteBranch(int id)
        {
            await _branchService.DeleteBranchAsync(id);

            return Ok(new ApiResponse<string>
            {
                Code = 200,
                Status = "Success",
                Result = "Branch has been permanently deleted."
            });
        }

        [HttpPut("SoftDelete/{id}")]
        public async Task<IActionResult> SoftDeleteBranch(int id)
        {
            await _branchService.SoftDeleteBranchAsync(id);

            return Ok(new ApiResponse<string>
            {
                Code = 200,
                Status = "Success",
                Result = "Branch has been soft deleted successfully."
            });
        }

        #endregion

    }
}

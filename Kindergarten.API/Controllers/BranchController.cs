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
            try
            {
                var result = await _branchService.GetAllBranchesAsync(filter);

                return Ok(new ApiResponse<PagedResult<BranchDTO>>
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
                    Result = $"An error occurred while retrieving branches: {ex.Message}"
                });
            }
        }

        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetBranchById(int id)
        {
            try
            {
                var branch = await _branchService.GetBranchByIdAsync(id);

                if (branch is null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Code = 404,
                        Status = "NotFound",
                        Result = $"Branch with ID {id} was not found."
                    });
                }

                return Ok(new ApiResponse<BranchDTO>
                {
                    Code = 200,
                    Status = "Success",
                    Result = branch
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Code = 500,
                    Status = "Error",
                    Result = $"An error occurred while retrieving the branch: {ex.Message}"
                });
            }
        }



        [HttpPost("Create")]
        public async Task<IActionResult> CreateBranch([FromBody] BranchCreateDTO branchCreateDto)
        {
            try
            {
                if (branchCreateDto == null)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Code = 400,
                        Status = "Error",
                        Result = "Request body cannot be null."
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
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Code = 500,
                    Status = "Error",
                    Result = $"An error occurred while creating the branch: {ex.Message}"
                });
            }
        }


        [HttpPut("Update")]
        public async Task<IActionResult> UpdateBranch([FromBody] BranchUpdateDTO branchUpdateDto)
        {
            try
            {
                if (branchUpdateDto == null)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Code = 400,
                        Status = "Error",
                        Result = "Request body cannot be null."
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
                        Code = 400,
                        Status = "Validation Error",
                        Result = errors
                    });
                }
                var createdBy = User.Identity?.Name ?? "Unknown";

                var updatedBranch = await _branchService.UpdateBranchAsync(branchUpdateDto, createdBy);
                if (updatedBranch == null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Code = 404,
                        Status = "Error",
                        Result = $"Branch with ID {branchUpdateDto.Id} was not found."
                    });
                }

                return Ok(new ApiResponse<BranchDTO>
                {
                    Code = 200,
                    Status = "Success",
                    Result = updatedBranch
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Code = 500,
                    Status = "Error",
                    Result = $"An error occurred while updating the branch: {ex.Message}"
                });
            }
        }


        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> DeleteBranch(int id)
        {
            try
            {
                var success = await _branchService.DeleteBranchAsync(id);

                if (!success)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Code = 404,
                        Status = "Error",
                        Result = $"Branch with ID {id} was not found."
                    });
                }

                return Ok(new ApiResponse<string>
                {
                    Code = 200,
                    Status = "Success",
                    Result = "Branch has been permanently deleted."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Code = 500,
                    Status = "Error",
                    Result = $"An error occurred while deleting the branch: {ex.Message}"
                });
            }
        }


        [HttpPut("SoftDelete/{id}")]
        public async Task<IActionResult> SoftDeleteBranch(int id)
        {
            try
            {
                var success = await _branchService.SoftDeleteBranchAsync(id);

                if (!success)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Code = 404,
                        Status = "Error",
                        Result = $"Branch with ID {id} was not found."
                    });
                }

                return Ok(new ApiResponse<string>
                {
                    Code = 200,
                    Status = "Success",
                    Result = "Branch has been soft deleted successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Code = 500,
                    Status = "Error",
                    Result = $"An error occurred while performing soft delete: {ex.Message}"
                });
            }
        }

        #endregion

    }
}

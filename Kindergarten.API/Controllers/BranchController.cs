using System.Net;
using Kindergarten.BLL.Helper;
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
        public async Task<IActionResult> GetAllBranches()
        {
            try
            {
                var branches = await _branchService.GetAllBranchesAsync();
                return Ok(new ApiResponse<IEnumerable<BranchDTO>>
                {
                    Code = (int)HttpStatusCode.OK,
                    Status = "Success",
                    Result = branches
                });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new ApiResponse<string>
                {
                    Code = (int)HttpStatusCode.InternalServerError,
                    Status = "Error",
                    Result = ex.Message
                });
            }
        }

        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetBranchById(int id)
        {
            try
            {
                var branch = await _branchService.GetBranchByIdAsync(id);
                if (branch == null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Code = (int)HttpStatusCode.NotFound,
                        Status = "Error",
                        Result = $"Branch with ID {id} not found."
                    });
                }

                return Ok(new ApiResponse<BranchDTO>
                {
                    Code = (int)HttpStatusCode.OK,
                    Status = "Success",
                    Result = branch
                });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new ApiResponse<string>
                {
                    Code = (int)HttpStatusCode.InternalServerError,
                    Status = "Error",
                    Result = ex.Message
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
                        Code = (int)HttpStatusCode.BadRequest,
                        Status = "Error",
                        Result = "Invalid data."
                    });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(new ApiResponse<object>
                    {
                        Code = (int)HttpStatusCode.BadRequest,
                        Status = "Validation Error",
                        Result = errors
                    });
                }

                var CreatedBy = User.Identity?.Name ?? "Unknown";
                var createdBranch = await _branchService.CreateBranchAsync(branchCreateDto, CreatedBy);

                var response = new ApiResponse<BranchDTO>  // هنا نوع الـ DTO الصحيح
                {
                    Code = (int)HttpStatusCode.Created,
                    Status = "Success",
                    Result = createdBranch
                };

                return CreatedAtAction(nameof(GetBranchById), new { id = createdBranch.Id }, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new ApiResponse<string>
                {
                    Code = (int)HttpStatusCode.InternalServerError,
                    Status = "Error",
                    Result = ex.Message
                });
            }
        }

        [HttpPut("Update")]
        public async Task<IActionResult> UpdateBranch([FromBody] BranchUpdateDTO branchUpdateDto)
        {
            if (branchUpdateDto == null)
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
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new ApiResponse<object>
                {
                    Code = (int)HttpStatusCode.BadRequest,
                    Status = "Validation Error",
                    Result = errors
                });
            }

            var updatedBranch = await _branchService.UpdateBranchAsync(branchUpdateDto);
            if (updatedBranch == null)
            {
                return NotFound(new ApiResponse<string>
                {
                    Code = (int)HttpStatusCode.NotFound,
                    Status = "Error",
                    Result = $"Branch with ID {branchUpdateDto.Id} not found."
                });
            }

            var response = new ApiResponse<BranchDTO>
            {
                Code = (int)HttpStatusCode.OK,
                Status = "Success",
                Result = updatedBranch
            };
            return Ok(response);
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
                        Code = (int)HttpStatusCode.NotFound,
                        Status = "Error",
                        Result = $"Branch with ID {id} not found."
                    });
                }

                return Ok(new ApiResponse<string>
                {
                    Code = (int)HttpStatusCode.OK,
                    Status = "Success",
                    Result = "Branch deleted successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new ApiResponse<string>
                {
                    Code = (int)HttpStatusCode.InternalServerError,
                    Status = "Error",
                    Result = ex.Message
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
                        Code = (int)HttpStatusCode.NotFound,
                        Status = "Error",
                        Result = $"Branch with ID {id} not found."
                    });
                }

                return Ok(new ApiResponse<string>
                {
                    Code = (int)HttpStatusCode.OK,
                    Status = "Success",
                    Result = "Branch soft deleted successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new ApiResponse<string>
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

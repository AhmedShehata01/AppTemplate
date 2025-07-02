using System.Net;
using System.Security.Claims;
using Kindergarten.BLL.Helper;
using Kindergarten.BLL.Models;
using Kindergarten.BLL.Models.ActivityLogDTO;
using Kindergarten.BLL.Models.KindergartenDTO;
using Kindergarten.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.API.Controllers
{
    [Authorize]
    public class KindergartenController : BaseController
    {
        #region prop
        private readonly IKindergartenService _kindergartenService;
        #endregion

        #region ctor
        public KindergartenController(IKindergartenService kindergartenService)
        {
            _kindergartenService = kindergartenService;
        }
        #endregion

        #region actions

        // GET: api/Kindergarten/GetAll
        [HttpGet("GetAllPaginated")]
        public async Task<IActionResult> GetAllPaginated([FromQuery] PaginationFilter filter)
        {
            try
            {
                var pagedResult = await _kindergartenService.GetAllKgsAsync(filter);

                return Ok(new ApiResponse<PagedResult<KindergartenDTO>>
                {
                    Code = (int)HttpStatusCode.OK,
                    Status = "Success",
                    Result = pagedResult
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

        // GET: api/Kindergarten/GetById/5
        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var kg = await _kindergartenService.GetKgByIdAsync(id);
                if (kg == null)
                    return NotFound(new ApiResponse<string>
                    {
                        Code = (int)HttpStatusCode.NotFound,
                        Status = "Error",
                        Result = $"Kindergarten with ID {id} not found."
                    });

                return Ok(new ApiResponse<KindergartenDTO>
                {
                    Code = (int)HttpStatusCode.OK,
                    Status = "Success",
                    Result = kg
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

        // POST: api/Kindergarten/Create
        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] KindergartenCreateDTO dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Code = (int)HttpStatusCode.BadRequest,
                        Status = "Validation Error",
                        Result = "Invalid data."
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
                        Code = (int)HttpStatusCode.BadRequest,
                        Status = "Validation Error",
                        Result = errors
                    });
                }


                var createdByUserName = User.Identity?.Name ?? "Unknown";
                var createdByUserId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                var createdKg = await _kindergartenService.CreateKgAsync(dto, createdByUserId, createdByUserName);

                return CreatedAtAction(nameof(GetById), new { id = createdKg.Id }, new ApiResponse<KindergartenDTO>
                {
                    Code = (int)HttpStatusCode.Created,
                    Status = "Success",
                    Result = createdKg
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Code = (int)HttpStatusCode.BadRequest,
                    Status = "Validation Error",
                    Result = ex.Message
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

        // PUT: api/Kindergarten/Update
        [HttpPut("Update")]
        public async Task<IActionResult> Update([FromBody] KindergartenUpdateDTO dto, string? userComment)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Code = (int)HttpStatusCode.BadRequest,
                        Status = "Validation Error",
                        Result = "Invalid data."
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
                        Code = (int)HttpStatusCode.BadRequest,
                        Status = "Validation Error",
                        Result = errors
                    });
                }

                // ✅ جلب UserId و UserName بنفس طريقة الـ Create
                var updatedByUserName = User.Identity?.Name ?? "Unknown";
                var updatedByUserId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(updatedByUserId))
                {
                    return Unauthorized(new ApiResponse<string>
                    {
                        Code = (int)HttpStatusCode.Unauthorized,
                        Status = "Unauthorized",
                        Result = "User Id is missing from token."
                    });
                }

                var updatedKg = await _kindergartenService.UpdateKgAsync(dto, updatedByUserId, updatedByUserName, userComment);

                if (updatedKg == null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Code = (int)HttpStatusCode.NotFound,
                        Status = "Error",
                        Result = $"Kindergarten with ID {dto.Id} not found."
                    });
                }

                return Ok(new ApiResponse<KindergartenDTO>
                {
                    Code = (int)HttpStatusCode.OK,
                    Status = "Success",
                    Result = updatedKg
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Code = (int)HttpStatusCode.BadRequest,
                    Status = "Validation Error",
                    Result = ex.Message
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


        // DELETE: api/Kindergarten/Delete/5
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id, string? userComment)
        {
            try
            {
                var userName = User.Identity?.Name ?? "Unknown";
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                var success = await _kindergartenService.DeleteKgAsync(id, userId, userName, userComment);

                if (!success)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Code = (int)HttpStatusCode.NotFound,
                        Status = "Error",
                        Result = $"Kindergarten with ID {id} not found."
                    });
                }

                return Ok(new ApiResponse<string>
                {
                    Code = (int)HttpStatusCode.OK,
                    Status = "Success",
                    Result = "Kindergarten deleted successfully."
                });
            }
            catch (DbUpdateException)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Code = (int)HttpStatusCode.BadRequest,
                    Status = "Delete Failed",
                    Result = "Cannot delete this kindergarten because it has related data."
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

        // PUT: api/Kindergarten/SoftDelete/5
        [HttpPut("SoftDelete/{id}")]
        public async Task<IActionResult> SoftDelete(int id, string? userComment)
        {
            try
            {
                var userName = User.Identity?.Name ?? "Unknown";
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                var success = await _kindergartenService.SoftDeleteKgWithBranchesAsync(id, userId, userName, userComment);

                if (!success)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Code = (int)HttpStatusCode.NotFound,
                        Status = "Error",
                        Result = $"Kindergarten with ID {id} not found."
                    });
                }

                return Ok(new ApiResponse<string>
                {
                    Code = (int)HttpStatusCode.OK,
                    Status = "Success",
                    Result = "Kindergarten soft deleted successfully."
                });
            }
            catch (DbUpdateException)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Code = (int)HttpStatusCode.BadRequest,
                    Status = "Delete Failed",
                    Result = "Cannot soft delete this kindergarten because it has related data."
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

        // GET: api/Kindergarten/{id}/History
        [HttpGet("History/{id}")]
        public async Task<IActionResult> GetKgHistory(int id)
        {
            try
            {
                var logs = await _kindergartenService.GetKgHistoryByKgIdAsync(id);

                if (logs == null || !logs.Any())
                {
                    return Ok(new ApiResponse<string>
                    {
                        Code = 404,
                        Status = "NotFound",
                        Result = "No logs found for this kindergarten."
                    });
                }

                return Ok(new ApiResponse<List<ActivityLogViewDTO>>
                {
                    Code = 200,
                    Status = "Success",
                    Result = logs
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

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
            var pagedResult = await _kindergartenService.GetAllKgsAsync(filter);

            return Ok(new ApiResponse<PagedResult<KindergartenDTO>>
            {
                Code = 200,
                Status = "Success",
                Result = pagedResult
            });
        }


        // GET: api/Kindergarten/GetById/5
        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var kg = await _kindergartenService.GetKgByIdAsync(id);

            return Ok(new ApiResponse<KindergartenDTO>
            {
                Code = 200,
                Status = "Success",
                Result = kg
            });
        }

        // PUT: api/Kindergarten/Create
        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] KindergartenCreateDTO dto)
        {
            if (dto == null)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Code = 400,
                    Status = "Validation Error",
                    Result = "Invalid data."
                });
            }

            var createdKg = await _kindergartenService.CreateKgAsync(dto, CurrentUserId, CurrentUserName);

            return CreatedAtAction(nameof(GetById), new { id = createdKg.Id }, new ApiResponse<KindergartenDTO>
            {
                Code = 201,
                Status = "Success",
                Result = createdKg
            });
        }


        // PUT: api/Kindergarten/Update
        [HttpPut("Update")]
        public async Task<IActionResult> Update([FromBody] KindergartenUpdateDTO dto, string? userComment)
        {
            if (dto == null)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Code = 400,
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
                    Code = 400,
                    Status = "Validation Error",
                    Result = errors
                });
            }

            var updatedKg = await _kindergartenService.UpdateKgAsync(dto, CurrentUserId, CurrentUserName, userComment);

            return Ok(new ApiResponse<KindergartenDTO>
            {
                Code = 200,
                Status = "Success",
                Result = updatedKg
            });
        }



        // DELETE: api/Kindergarten/Delete/5
        // DELETE: api/Kindergarten/Delete/5
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id, string? userComment)
        {
            var success = await _kindergartenService.DeleteKgAsync(id, CurrentUserId, CurrentUserName, userComment);

            return Ok(new ApiResponse<string>
            {
                Code = 200,
                Status = "Success",
                Result = "Kindergarten deleted successfully."
            });
        }


        // PUT: api/Kindergarten/SoftDelete/5
        [HttpPut("SoftDelete/{id}")]
        public async Task<IActionResult> SoftDelete(int id, string? userComment)
        {
            var success = await _kindergartenService.SoftDeleteKgWithBranchesAsync(id, CurrentUserId, CurrentUserName, userComment);

            return Ok(new ApiResponse<string>
            {
                Code = 200,
                Status = "Success",
                Result = "Kindergarten soft deleted successfully."
            });
        }


        // GET: api/Kindergarten/History/{id}
        [HttpGet("History/{id}")]
        public async Task<IActionResult> GetKgHistory(int id)
        {
            var logs = await _kindergartenService.GetKgHistoryByKgIdAsync(id);

            if (!logs.Any())
            {
                return NotFound(new ApiResponse<string>
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


        #endregion

    }
}

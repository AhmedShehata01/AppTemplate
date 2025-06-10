using System.Net;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Kindergarten.BLL.Helper;
using Kindergarten.BLL.Models.KindergartenDTO;
using Kindergarten.BLL.Services;

namespace Kindergarten.API.Controllers
{
    [Authorize]
    public class KindergartenController : BaseController
    {
        private readonly IKindergartenService _kindergartenService;

        public KindergartenController(IKindergartenService kindergartenService)
        {
            _kindergartenService = kindergartenService;
        }

        // GET: api/Kindergarten/GetAll
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                // بدّل السطر هذا
                var kgs = await _kindergartenService.GetAllKgsWithBranchesAsync();
                return Ok(new ApiResponse<IEnumerable<KindergartenDTO>>
                {
                    Code = (int)HttpStatusCode.OK,
                    Status = "Success",
                    Result = kgs
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
                    return BadRequest(new ApiResponse<string>
                    {
                        Code = (int)HttpStatusCode.BadRequest,
                        Status = "Error",
                        Result = "Invalid data."
                    });

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

                var createdBy = User.Identity?.Name ?? "Unknown";
                var createdKg = await _kindergartenService.CreateKgAsync(dto, createdBy);

                return CreatedAtAction(nameof(GetById), new { id = createdKg.Id }, new ApiResponse<KindergartenDTO>
                {
                    Code = (int)HttpStatusCode.Created,
                    Status = "Success",
                    Result = createdKg
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
        public async Task<IActionResult> Update([FromBody] KindergartenUpdateDTO dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new ApiResponse<string>
                    {
                        Code = (int)HttpStatusCode.BadRequest,
                        Status = "Error",
                        Result = "Invalid data."
                    });

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

                var updatedKg = await _kindergartenService.UpdateKgAsync(dto);
                if (updatedKg == null)
                    return NotFound(new ApiResponse<string>
                    {
                        Code = (int)HttpStatusCode.NotFound,
                        Status = "Error",
                        Result = $"Kindergarten with ID {dto.Id} not found."
                    });

                return Ok(new ApiResponse<KindergartenDTO>
                {
                    Code = (int)HttpStatusCode.OK,
                    Status = "Success",
                    Result = updatedKg
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
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _kindergartenService.DeleteKgAsync(id);
                if (!success)
                    return NotFound(new ApiResponse<string>
                    {
                        Code = (int)HttpStatusCode.NotFound,
                        Status = "Error",
                        Result = $"Kindergarten with ID {id} not found."
                    });

                return Ok(new ApiResponse<string>
                {
                    Code = (int)HttpStatusCode.OK,
                    Status = "Success",
                    Result = "Kindergarten deleted successfully."
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
        public async Task<IActionResult> SoftDelete(int id)
        {
            try
            {
                var success = await _kindergartenService.SoftDeleteKgAsync(id);
                if (!success)
                    return NotFound(new ApiResponse<string>
                    {
                        Code = (int)HttpStatusCode.NotFound,
                        Status = "Error",
                        Result = $"Kindergarten with ID {id} not found."
                    });

                return Ok(new ApiResponse<string>
                {
                    Code = (int)HttpStatusCode.OK,
                    Status = "Success",
                    Result = "Kindergarten soft deleted successfully."
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
    }
}

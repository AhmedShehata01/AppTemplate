using System.Net;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Kindergarten.BLL.Helper;
using Kindergarten.BLL.Models.KindergartenDTO;
using Kindergarten.BLL.Services;
using Kindergarten.BLL.Models;
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

                var createdBy = User.Identity?.Name ?? "Unknown";
                var createdKg = await _kindergartenService.CreateKgAsync(dto, createdBy);

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
        public async Task<IActionResult> Update([FromBody] KindergartenUpdateDTO dto)
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

                var updatedBy = User.Identity?.Name ?? "Unknown";
                var updatedKg = await _kindergartenService.UpdateKgAsync(dto, updatedBy);

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
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                // 🟡 محاولة حذف الحضانة من خلال الـ Service
                var success = await _kindergartenService.DeleteKgAsync(id);

                // 🔴 لو الحضانة غير موجودة
                if (!success)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Code = (int)HttpStatusCode.NotFound,
                        Status = "Error",
                        Result = $"Kindergarten with ID {id} not found."
                    });
                }

                // ✅ حذف ناجح
                return Ok(new ApiResponse<string>
                {
                    Code = (int)HttpStatusCode.OK,
                    Status = "Success",
                    Result = "Kindergarten deleted successfully."
                });
            }
            catch (DbUpdateException)
            {
                // 🔴 خطأ بسبب وجود علاقات مرتبطة (مثل: فروع، موظفين...)
                return BadRequest(new ApiResponse<string>
                {
                    Code = (int)HttpStatusCode.BadRequest,
                    Status = "Delete Failed",
                    Result = "Cannot delete this kindergarten because it has related data."
                });
            }
            catch (Exception ex)
            {
                // 🔴 أي خطأ عام غير متوقع
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
            var updatedBy = User.Identity?.Name ?? "Unknown";

            try
            {
                // 🟡 محاولة Soft Delete من خلال الـ Service
                var success = await _kindergartenService.SoftDeleteKgWithBranchesAsync(id , updatedBy);

                // 🔴 الحضانة غير موجودة
                if (!success)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Code = (int)HttpStatusCode.NotFound,
                        Status = "Error",
                        Result = $"Kindergarten with ID {id} not found."
                    });
                }

                // ✅ Soft Delete ناجح
                return Ok(new ApiResponse<string>
                {
                    Code = (int)HttpStatusCode.OK,
                    Status = "Success",
                    Result = "Kindergarten soft deleted successfully."
                });
            }
            catch (DbUpdateException)
            {
                // 🔴 وجود بيانات مرتبطة تمنع الحذف الناعم (لو طبقت نفس منطق العلاقات)
                return BadRequest(new ApiResponse<string>
                {
                    Code = (int)HttpStatusCode.BadRequest,
                    Status = "Delete Failed",
                    Result = "Cannot soft delete this kindergarten because it has related data."
                });
            }
            catch (Exception ex)
            {
                // 🔴 أي خطأ غير متوقع
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

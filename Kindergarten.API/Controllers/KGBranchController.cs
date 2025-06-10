using Kindergarten.BLL.Helper;
using System.Net;
using Kindergarten.BLL.Models.KGBranchDTO;
using Kindergarten.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kindergarten.API.Controllers
{
    [Authorize]
    public class KGBranchController : BaseController
    {
        #region Prop
        private readonly IKGBranchService _kgBranchService;
        private readonly IKindergartenService _kindergartenService;
        #endregion

        #region CTOR
        public KGBranchController(IKGBranchService kgBranchService,
                                    IKindergartenService kindergartenService)
        {
            _kgBranchService = kgBranchService;
            _kindergartenService = kindergartenService;
        }
        #endregion

        #region Actions
        // GET: api/KGBranch
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _kgBranchService.GetAllKgWithBranchesAsync();
                return Ok(new ApiResponse<List<KGBranchDTO>>
                {
                    Code = (int)HttpStatusCode.OK,
                    Status = "Success",
                    Result = result
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

        // GET: api/KGBranch/5
        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _kgBranchService.GetKgWithBranchesByIdAsync(id);
                if (result == null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Code = (int)HttpStatusCode.NotFound,
                        Status = "Error",
                        Result = $"KG with ID {id} not found."
                    });
                }

                return Ok(new ApiResponse<KGBranchDTO>
                {
                    Code = (int)HttpStatusCode.OK,
                    Status = "Success",
                    Result = result
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

        // POST: api/KGBranch
        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] KGBranchCreateDTO dto)
        {
            try
            {
                if (dto == null)
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

                // 🔥 استخرج الـ CreatedBy من الـ JWT Token
                var createdBy = User?.Identity?.Name;

                // أو إذا كان موجود كـ Claim مخصص:
                // var createdBy = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

                if (string.IsNullOrEmpty(createdBy))
                {
                    return Unauthorized(new ApiResponse<string>
                    {
                        Code = (int)HttpStatusCode.Unauthorized,
                        Status = "Error",
                        Result = "Unauthorized: User identity is missing."
                    });
                }

                var created = await _kgBranchService.CreateKgWithBranchesAsync(dto, createdBy);
                return CreatedAtAction(nameof(GetById), new { id = created.Kg.Id }, new ApiResponse<KGBranchDTO>
                {
                    Code = (int)HttpStatusCode.Created,
                    Status = "Success",
                    Result = created
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


        // Controller: KGBranchController.cs
        [HttpPut("Update")]
        public async Task<IActionResult> Update([FromBody] KGBranchUpdateDTO dto)
        {
            try
            {
                if (dto == null)
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

                // Extract CreatedBy from JWT
                var createdBy = User?.Identity?.Name;
                if (string.IsNullOrEmpty(createdBy))
                {
                    return Unauthorized(new ApiResponse<string>
                    {
                        Code = (int)HttpStatusCode.Unauthorized,
                        Status = "Error",
                        Result = "Unauthorized: User identity is missing."
                    });
                }

                var updated = await _kgBranchService.UpdateKgWithBranchesAsync(dto, createdBy);
                if (updated == null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Code = (int)HttpStatusCode.NotFound,
                        Status = "Error",
                        Result = $"KG with ID {dto.Kg.Id} not found."
                    });
                }

                return Ok(new ApiResponse<KGBranchDTO>
                {
                    Code = (int)HttpStatusCode.OK,
                    Status = "Success",
                    Result = updated
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

        // DELETE: api/KGBranch/5
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _kgBranchService.DeleteKgWithBranchesAsync(id);
                if (!result)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Code = (int)HttpStatusCode.NotFound,
                        Status = "Error",
                        Result = $"KG with ID {id} not found."
                    });
                }

                return Ok(new ApiResponse<string>
                {
                    Code = (int)HttpStatusCode.OK,
                    Status = "Success",
                    Result = "KG deleted successfully."
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

        // PATCH: api/KGBranch/softdelete/5
        [HttpPut("SoftDelete/{id}")]
        public async Task<IActionResult> SoftDelete(int id)
        {
            try
            {
                var result = await _kgBranchService.SoftDeleteKgWithBranchesAsync(id);
                if (!result)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Code = (int)HttpStatusCode.NotFound,
                        Status = "Error",
                        Result = $"KG with ID {id} not found."
                    });
                }

                return Ok(new ApiResponse<string>
                {
                    Code = (int)HttpStatusCode.OK,
                    Status = "Success",
                    Result = "KG soft deleted successfully."
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

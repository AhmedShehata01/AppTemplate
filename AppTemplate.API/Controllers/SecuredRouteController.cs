using System.Security.Claims;
using AppTemplate.BLL.Helper;
using AppTemplate.BLL.Models;
using AppTemplate.BLL.Models.DRBRADTO;
using AppTemplate.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTemplate.API.Controllers
{
    [Authorize(Roles = "Super Admin,Admin")]
    public class SecuredRouteController : BaseController
    {
        #region Prop
        private readonly ISecuredRouteService _securedRouteService;
        #endregion

        #region CTOR
        public SecuredRouteController(ISecuredRouteService securedRouteService)
        {
            _securedRouteService = securedRouteService;
        }
        #endregion

        #region Actions 

        [HttpGet("GetAllPaginated")]
        public async Task<IActionResult> GetAllPaginated([FromQuery] PaginationFilter filter)
        {
            var pagedResult = await _securedRouteService.GetAllRoutesAsync(filter);

            return Ok(new ApiResponse<PagedResult<SecuredRouteDTO>>
            {
                Code = StatusCodes.Status200OK,
                Status = "Success",
                Result = pagedResult
            });
        }

        [HttpGet("GetById/{id}")]
        public async Task<ActionResult<ApiResponse<SecuredRouteDTO>>> GetById(int id)
        {
            // نترك الـService يرمي KeyNotFoundException لو لم يجد المسار
            var dto = await _securedRouteService.GetRouteByIdAsync(id);

            var response = new ApiResponse<SecuredRouteDTO>
            {
                Code = StatusCodes.Status200OK,
                Status = "Success",
                Result = dto
            };

            return Ok(response);
        }

        [HttpPost("Create")]
        public async Task<ActionResult<ApiResponse<int>>> Create([FromBody] CreateSecuredRouteDTO dto)
        {
            // 1. التحقق من صحة المدخلات
            if (!ModelState.IsValid)
                return ValidationProblem();

            // 2. استدعاء الService
            var id = await _securedRouteService.CreateRouteAsync(dto, CurrentUserId, CurrentUserName);

            // 3. إرجاع 201 Created مع الـ Location ورقم المعرف
            var response = new ApiResponse<int>
            {
                Code = StatusCodes.Status201Created,
                Status = "Created",
                Result = id
            };

            return CreatedAtAction(nameof(GetById), new { id }, response);
        }


        [HttpPut("update")]
        public async Task<ActionResult<ApiResponse<bool>>> Update([FromBody] UpdateSecuredRouteDTO dto)
        {
            // 1. تحقق من صحة المدخلات ضمن ApiResponse موحد
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new ApiResponse<List<string>>
                {
                    Code = StatusCodes.Status400BadRequest,
                    Status = "ValidationError",
                    Result = errors
                });
            }

            // 2. دع الـService يرمي Exceptions المناسبة (KeyNotFoundException → 404, InvalidOperationException → 400, ...)
            await _securedRouteService.UpdateRouteAsync(dto, CurrentUserId, CurrentUserName);

            // 3. إعادة النجاح
            return Ok(new ApiResponse<bool>
            {
                Code = StatusCodes.Status200OK,
                Status = "Success",
                Result = true
            });
        }



        [HttpDelete("delete/{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
        {
            // 1. ندع الـService يرمي KeyNotFoundException لو المسار غير موجود
            await _securedRouteService.DeleteRouteAsync(id, CurrentUserId, CurrentUserName);

            // 2. نعيد ApiResponse موحّد
            return Ok(new ApiResponse<bool>
            {
                Code = StatusCodes.Status200OK,
                Status = "Deleted",
                Result = true
            });
        }


        [HttpPost("assign-roles")]
        public async Task<ActionResult<ApiResponse<bool>>> AssignRoles([FromBody] AssignRolesToRouteDTO dto)
        {
            // 1. التحقق من صحة بيانات الـ DTO
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new ApiResponse<List<string>>
                {
                    Code = StatusCodes.Status400BadRequest,
                    Status = "ValidationError",
                    Result = errors
                });
            }

            // 2. دع الـService يرمي Exceptions مناسبة (ArgumentNullException أو KeyNotFoundException)
            await _securedRouteService.AssignRolesAsync(dto, CurrentUserId, CurrentUserName);

            // 3. إعادة ApiResponse ناجح
            return Ok(new ApiResponse<bool>
            {
                Code = StatusCodes.Status200OK,
                Status = "RolesAssigned",
                Result = true
            });
        }


        [HttpPost("unassign-role")]
        public async Task<ActionResult<ApiResponse<bool>>> UnassignRole([FromBody] UnassignRoleFromRouteDTO dto)
        {
            // 1. التحقق من صحة بيانات الـ DTO
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new ApiResponse<List<string>>
                {
                    Code = StatusCodes.Status400BadRequest,
                    Status = "ValidationError",
                    Result = errors
                });
            }

            // 2. استدعاء الـ Service (سيرمي ArgumentNullException أو KeyNotFoundException على حسب الحالة)
            await _securedRouteService.UnassignRoleAsync(dto, CurrentUserId, CurrentUserName);

            // 3. إعادة ApiResponse ناجح
            return Ok(new ApiResponse<bool>
            {
                Code = StatusCodes.Status200OK,
                Status = "RoleUnassigned",
                Result = true
            });
        }


        [HttpGet("routes-with-roles")]
        public async Task<ActionResult<ApiResponse<List<RouteWithRolesDTO>>>> GetRoutesWithRoles()
        {
            // ندع الـ Service يرمي KeyNotFoundException لو القائمة فارغة (404)
            var result = await _securedRouteService.GetRoutesWithRolesAsync();

            // نعيد ApiResponse مغلف دومًا
            return Ok(new ApiResponse<List<RouteWithRolesDTO>>
            {
                Code = StatusCodes.Status200OK,
                Status = "Success",
                Result = result
            });
        }

        #endregion
    }
}

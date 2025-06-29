using Kindergarten.BLL.Helper;
using Kindergarten.BLL.Models.ActivityLogDTO;
using Kindergarten.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kindergarten.API.Controllers
{
    [Authorize]
    public class ActivityLogController : BaseController
    {
        #region Prop
        private readonly IActivityLogService _activityLogService;
        #endregion

        #region Ctor
        public ActivityLogController(IActivityLogService activityLogService)
        {
            _activityLogService = activityLogService;
        }
        #endregion

        #region Actions

        [HttpGet("api/activity-log/entity-history")]
        public async Task<IActionResult> GetEntityHistory([FromQuery] string entityName, [FromQuery] string entityId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(entityName) || string.IsNullOrWhiteSpace(entityId))
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Code = 400,
                        Status = "Error",
                        Result = "EntityName and EntityId are required."
                    });
                }

                var logs = await _activityLogService.GetEntityHistoryAsync(entityName, entityId);

                if (logs == null || !logs.Any())
                {
                    return Ok(new ApiResponse<string>
                    {
                        Code = 404,
                        Status = "NotFound",
                        Result = "No logs found for this entity."
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

        [HttpGet("api/activity-log/user-actions")]
        public async Task<IActionResult> GetUserActions(
            [FromQuery] string userId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Code = 400,
                        Status = "Error",
                        Result = "UserId is required."
                    });
                }

                var logs = await _activityLogService.GetUserActionsAsync(userId, fromDate, toDate);

                if (logs == null || !logs.Any())
                {
                    return Ok(new ApiResponse<string>
                    {
                        Code = 404,
                        Status = "NotFound",
                        Result = "No logs found for this user."
                    });
                }

                return Ok(new ApiResponse<List<ActivityLogDTO>>
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

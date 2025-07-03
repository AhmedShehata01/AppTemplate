using Kindergarten.BLL.Helper;
using Kindergarten.BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kindergarten.API.Controllers
{
    public class OtpController : BaseController
    {
        private readonly IOtpService _otpService;

        public OtpController(IOtpService otpService)
        {
            _otpService = otpService;
        }

        [HttpPost("GenerateOtp")]
        public async Task<IActionResult> GenerateOtp([FromBody] RequestOtpDTO dto)
        {
            var result = await _otpService.GenerateAndSendOtpAsync(dto);
            if (!result.Success)
                return BadRequest(new ApiResponse<string>
                { Code = 400, Status = "Error", Result = result.Message });
            return Ok(new ApiResponse<OtpDTO>
            { Code = 200, Status = "Success", Result = result.Data });
        }

        [HttpPost("VerifyOtp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDTO dto)
        {
            var result = await _otpService.VerifyOtpAsync(dto);
            if (!result.Success)
                return BadRequest(new ApiResponse<string>
                { Code = 400, Status = "Error", Result = result.Message });
            return Ok(new ApiResponse<OtpDTO>
            { Code = 200, Status = "Success", Result = result.Data });
        }

    }
}

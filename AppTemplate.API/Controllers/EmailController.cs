using AppTemplate.BLL.Helper;
using AppTemplate.BLL.Services.SendEmail;
using Microsoft.AspNetCore.Mvc;

namespace AppTemplate.API.Controllers
{
    public class EmailController : BaseController
    {
        private readonly IEmailService _emailService;

        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendEmail([FromBody] EmailDto request)
        {
            if (string.IsNullOrWhiteSpace(request.To) || string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Body))
            {
                return BadRequest(new ApiResponse<string>
                {
                    Code = 400,
                    Status = "BadRequest",
                    Result = "All fields are required."
                });
            }

            try
            {
                await _emailService.SendEmailAsync(request.To, request.Subject, request.Body);

                return Ok(new ApiResponse<string>
                {
                    Code = 200,
                    Status = "Success",
                    Result = "Email sent successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Code = 500,
                    Status = "Error",
                    Result = $"An error occurred: {ex.Message}"
                });
            }
        }

    }
}

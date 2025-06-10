using Kindergarten.BLL.Helper;
using Kindergarten.BLL.Services.SendEmail;
using Microsoft.AspNetCore.Mvc;

namespace Kindergarten.API.Controllers
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
                return BadRequest("All fields are required.");

            try
            {
                await _emailService.SendEmailAsync(request.To, request.Subject, request.Body);
                return Ok("Email sent successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}

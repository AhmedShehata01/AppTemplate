using Kindergarten.BLL.Models.Auth;
using Kindergarten.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Kindergarten.API.Controllers
{
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.RegisterAsync(model);

            if (result.Code != 200)
                return BadRequest(new { message = result.Result });

            return Ok(new { message = result.Result });
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.LoginAsync(model);

            if (result.Code != 200)
                return Unauthorized(new { message = result.Result });

            return Ok(new { token = result.Result });
        }

        // POST: /api/auth/change-password
        [HttpPost("changePassword")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var result = await _authService.ChangePasswordAsync(userId, model);
            return StatusCode(result.Code, result);
        }



        // AuthController.cs
        [HttpPost("external/google")]
        public async Task<IActionResult> GoogleLogin([FromBody] ExternalLoginRequestDto model)
        {
            var externalUser = await _authService.VerifyGoogleTokenAsync(model.IdToken);

            if (externalUser == null)
                return Unauthorized(new { message = "Invalid Google token" });

            var authResponse = await _authService.HandleExternalUserAsync(externalUser);
            return Ok(authResponse);
        }

        // POST: api/auth/external/facebook
        [HttpPost("external/facebook")]
        public async Task<IActionResult> FacebookLogin([FromBody] ExternalLoginRequestDto model)
        {
            var externalUser = await _authService.VerifyFacebookTokenAsync(model.IdToken);

            if (externalUser == null)
                return Unauthorized(new { message = "Invalid Facebook token" });

            var authResponse = await _authService.HandleExternalUserAsync(externalUser);
            return Ok(authResponse);
        }

        // GET: api/auth/me
        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var userName = User.Identity?.Name;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            // var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

            return Ok(new
            {
                userName,
                email,
                // roles
            });
        }

    }

}

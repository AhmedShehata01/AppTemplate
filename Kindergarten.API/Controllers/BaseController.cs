using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Kindergarten.API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class BaseController : ControllerBase
    {
        public string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
        public string CurrentUserName => User.Identity?.Name ?? "";
    }
}

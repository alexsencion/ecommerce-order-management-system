using ECommerce.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        protected IActionResult ToResponse<T>(Result<T> result)
        {
            if (result.IsSuccess)
                return StatusCode(result.StatusCode, result.Value);

            return result.StatusCode switch
            {
                400 => BadRequest(new { error = result.Error }),
                404 => NotFound(new { error = result.Error }),
                409 => Conflict(new { error = result.Error }),
                422 => UnprocessableEntity(new { error = result.Error }),
                _ => StatusCode(result.StatusCode, new { error = result.Error })
            };
        }
    }
}

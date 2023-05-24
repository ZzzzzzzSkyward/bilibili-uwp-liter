using Microsoft.AspNetCore.Mvc;

namespace BiliLite.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public JsonResult Get()
        {
            return new JsonResult(new
            {
                code = 0,
                message = "Hello,World!"
            });
        }
    }
}
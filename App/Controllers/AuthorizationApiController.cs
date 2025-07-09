using EasyAuth;
using EasyAuth.Utils.PasswordUtil;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Security.Claims;

namespace App.Controllers
{
    [Route("api/oauth")]
    [ApiController]
    public class AuthorizationApiController : ControllerBase

    {
        private readonly IConfiguration _configuration;
        public AuthorizationApiController(
                IConfiguration configuration
            )
        {
            _configuration = configuration;
        }
        [Authorize]
        [HttpGet("secret")]
        public ActionResult Secret()
        {

            var cl = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            return Ok($"Your login: {User.Identity.Name}");
        }


        
        [Authorize(Roles = "adminchik,terminator")]
        [VerifiedUserFilter]
        [HttpGet("super_admin")]
        //[ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        public ActionResult SecretSuperAdmin()
        {
            return Ok($"Your're allowed as super admin");
        }
    }
}

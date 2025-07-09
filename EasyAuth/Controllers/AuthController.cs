using EasyAuth.Dtos.User;
using EasyAuth.Interfaces;
using EasyAuth.Utils;
using EasyAuth.Utils.PasswordUtil;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static EasyAuth.EmailingService;

namespace EasyAuth.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthUserRepository _userRepository;
        private readonly AuthTokenizationService _tokenizationService;
        private readonly IConfiguration _configuration;
        private readonly EmailingService _emailingService;
        private readonly bool _cookieHttpOnly;
        private readonly bool _cookieSecure;
        private readonly bool _useResponseBody;
        private readonly bool _useCookies;
        private readonly int _authRefreshTokenExpirationMinutes;
        public AuthController(IAuthUserRepository userRepository,
                AuthTokenizationService tokenizationService,
                IConfiguration configuration,
                EmailingService emailingService)
        {
            _userRepository = userRepository;
            _tokenizationService = tokenizationService;
            _configuration = configuration;
            _emailingService = emailingService;

            var authConfigPart = configuration.GetSection("AuthSettings");

            _useResponseBody = authConfigPart.GetValue("UseResponseBody", true);
            _useCookies = authConfigPart.GetValue("UseCookies", false);
            _cookieHttpOnly = authConfigPart.GetValue("CookieParameters:HttpOnly", false);
            _cookieSecure = authConfigPart.GetValue("CookieParameters:Secure", false);
            _authRefreshTokenExpirationMinutes = authConfigPart.GetValue("AuthRefreshTokenExpirationMinutes", 10);

        }

        [HttpPost($"request_recovery")]
        [AllowAnonymous]
        public async Task<ActionResult> RequestRecovery([FromQuery] string type, [FromQuery] string value)
        {
            if (type == null || value == null) return BadRequest("type parameter must be equal to 'username' or 'email'. value parameter can't be empty");
            string foundUsername;
            string foundEmail;

            if (type == "email")
            {
                var foundUsers = await _userRepository.GetUsersByEmail(value);
                var foundUser = foundUsers.FirstOrDefault(p => p.IsVerified == true);

                if (foundUser == null) return BadRequest("user wasn't found for provided email or email wasn't verified");

                foundUsername = foundUser.Username;
                foundEmail = foundUser.Email;
            }
            else if (type == "username")
            {
                var foundUser = await _userRepository.GetUserByName(value);
                if (foundUser == null) return BadRequest("user wasn't found by username");

                foundUsername = foundUser.Username;
                foundEmail = foundUser.Email;
            }
            else
            {
                return BadRequest($"invalid type {type}, allowed 'username', 'email'");
            }
            var ci = AuthTokenizationService.GetIdentity(foundUsername, foundEmail);
            var url = await _emailingService.SendRecoveryEmail(foundEmail, foundUsername, ci);
            return new JsonResult(new { url= url }) { StatusCode = 200 };
        }

        [HttpGet("recovery")]
        [AllowAnonymous]
        public IActionResult GetRecoveryPage([FromQuery] string recoveryToken)
        {
            var claimsPrincipal = ClaimsPrincipalFromURLToken(recoveryToken, EmailTypes.Recovery);
            if (claimsPrincipal == null) return NotFound("Page not found");

            var baseurl = _configuration["AuthSettings:BaseUrl"];
            return Redirect($"{baseurl}/recovery.html?recoveryToken={recoveryToken}");
        }

        [HttpPatch($"recovery")]
        [AllowAnonymous]
        public async Task<ActionResult> PerformRecovery([FromQuery] string recoveryToken, [FromBody] RecoverPasswordDto recoverPasswordDto)
        {
            var claimsPrincipal = ClaimsPrincipalFromURLToken(recoveryToken, EmailTypes.Recovery);
            if (claimsPrincipal == null) return NotFound("Page not found");
            var username = claimsPrincipal.Identity.Name;
            var password = PasswordHasher.Hashify(recoverPasswordDto.RecoveryPassword, username);

            await _userRepository.SetUserPassword(username, password);
            return Ok("recovery successful");
        }


        [HttpPatch("password_change")]
        [AllowAnonymous]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            var users = await _userRepository.GetUsersByEmail(changePasswordDto.Email);
            var foundUser = users.FirstOrDefault(user => user.IsVerified == true);

            if (PasswordHasher.PasswordsMatch(foundUser.Password, foundUser.Username, changePasswordDto.OldPassword))
            {
                await _userRepository.SetUserPassword(
                    foundUser.Username,
                    PasswordHasher.Hashify(changePasswordDto.NewPassword, foundUser.Username)
                 );
                return Ok();
            }
            return Unauthorized("passwords don't match");

        }


        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        public async Task<ActionResult> CreateUser(UserCreateDto userCreateDto, CancellationToken cancellationToken)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest("validation errors found");
            }

            var protectedPassword = PasswordHasher.Hashify(userCreateDto.Password, userCreateDto.Username);

            var apiUserModel = userCreateDto.Adapt<IAuthUser>();
            apiUserModel.Password = protectedPassword;

            await _userRepository.CreateUser(apiUserModel);

            List<string> userRoles = await _userRepository.GetUserRoles(apiUserModel.Username);
            var tokenResponse = await _userRepository.AuthorizeWithPasswordGetToken(apiUserModel, userRoles, userCreateDto.Password);

            if (tokenResponse == null) return BadRequest("malformed token");

            CreateSessionCookieIfSendCookieTrue(tokenResponse);

            //await _emailingService.SendVerificationEmail(
            //        apiUserModel.Email,
            //        apiUserModel.Username,
            //        userRoles);

            return Ok(_useResponseBody == true ? tokenResponse : null);
        }

        [HttpPost($"request_verification")]
        [Authorize]
        public async Task<IActionResult> RequestVerification(CancellationToken cancellationToken)
        {
            var username = User.Identity.Name;
            var email = User.FindFirst(ClaimTypes.Email).Value;
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            await _emailingService.SendVerificationEmail(
                    email,
                    username,
                    roles
                    );

            return Ok("Verification was requested");
        }

        [HttpGet("roles")]
        [Authorize]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _userRepository.GetUserRoles(User.Identity.Name);
            return new JsonResult(new { roles }) { StatusCode = 200 };
        }

        private ClaimsPrincipal ClaimsPrincipalFromURLToken(string token, EmailTypes emailType = EmailTypes.Verification)
        {
            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            var claimsPrincipal = _emailingService.ValidateTokenGetPrincipal(decodedToken, emailType);
            return claimsPrincipal;
        }

        [HttpPatch($"verification/{{verificationToken}}")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyProfile(string verificationToken)
        {
            var claimsPrincipal = ClaimsPrincipalFromURLToken(verificationToken);
            if (claimsPrincipal == null) return NotFound("Page not found");

            string username = claimsPrincipal.FindFirst(ClaimTypes.Name).Value;
            string email = claimsPrincipal.FindFirst(ClaimTypes.Email).Value;

            await _userRepository.SetUserEmailVerified(username, email);
            return Ok("User verified :)");
        }

        private void CreateSessionCookieIfSendCookieTrue(TokenModel tokenModel)
        {
            if (_useCookies == true)
            {
                var cookieOptions = new CookieOptions()
                {
                    Path = "/",
                    IsEssential = true,
                    HttpOnly = _cookieHttpOnly,
                    Secure = _cookieSecure
                };
                HttpContext.Response.Cookies.Append("session", tokenModel.AccessToken, cookieOptions);
                HttpContext.Response.Cookies.Append("refresh_session", tokenModel.RefreshToken, cookieOptions);
            }
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(TokenModel), (int)HttpStatusCode.OK)]
        public async Task<ActionResult> Login(UserCreateDto userCreateDto)
        {
            var inputPassword = userCreateDto.Password;

            var foundUser = await _userRepository.GetUserByName(userCreateDto.Username);

            if (foundUser == null) { return Ok("User not found"); }

            List<string> userRoles = await _userRepository.GetUserRoles(foundUser.Username);
            var tokenResponse = await _userRepository.AuthorizeWithPasswordGetToken(foundUser, userRoles, inputPassword);

            if (tokenResponse == null) return Unauthorized("Password not validated :(");


            CreateSessionCookieIfSendCookieTrue(tokenResponse);

            return Ok(_useResponseBody == true ? tokenResponse : null);
        }

        //[HttpPost("all_roles")]
        //[ProducesResponseType(typeof(List<Role>), (int)HttpStatusCode.OK)]
        //public async Task<IActionResult> AllRoles()
        //{
        //    return Ok("you accessed secrets");
        //}


        [HttpPost("refresh")]
        public async Task<ActionResult> Refresh([FromBody] TokenModel tokenModel)
        {
            if (tokenModel == null)
            {
                return BadRequest("must provide access and refresh token");
            }

            string accessToken = tokenModel.AccessToken;
            string refreshToken = tokenModel.RefreshToken;

            var principal = _tokenizationService.ValidateTokenGetPrincipal(accessToken);

            if (principal == null)
            {
                return BadRequest("Refresh token not validated");
            }

            var username = principal.Identity.Name;

            var user = await _userRepository.GetUserByName(username);
            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpirationUTC <= DateTime.UtcNow)
            {
                return BadRequest("Invalid client credentials: bad refresh token or user not found");
            }

            List<string> userRoles = await _userRepository.GetUserRoles(user.Username);
            var identity = AuthTokenizationService.GetIdentity(user.Username, user.Email, user.IsVerified, userRoles);
            var newAccessToken = _tokenizationService.GenerateAccessToken(identity);
            var newRefreshToken = _tokenizationService.GenerateRefreshToken();

            var modifiedTime = DateTime.UtcNow.AddMinutes(_authRefreshTokenExpirationMinutes);
            await _userRepository.SetRefreshTokenForUser(
                name: user.Username,
                refreshToken: newRefreshToken,
                modifiedTime
            );

            return new ObjectResult(new
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshToken
            });
        }

        [Authorize]
        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke()
        {
            var username = User.Identity.Name;

            await _userRepository.RevokeRefreshTokenForUser(username); //revoke token
            return NoContent();
        }
    }
}

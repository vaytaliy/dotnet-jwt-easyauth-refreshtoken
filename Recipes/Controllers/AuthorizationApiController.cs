using EasyAuth;
using EasyAuth.Utils.PasswordUtil;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Recipes.Dtos.User;
using Recipes.Models;
using Recipes.Repositories;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Recipes.Controllers
{
    [Route("api/oauth")]
    [ApiController]
    public class AuthorizationApiController : ControllerBase

    {
        private readonly UserRepository _userRepository;
        public AuthorizationApiController(
            UserRepository userRepository
            )
        {
            _userRepository = userRepository;
        }
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult> CreateUser(UserCreateDto userCreateDto)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest("validation errors found");
            }

            var protectedPassword = PasswordHasher.Hashify(userCreateDto.Password, userCreateDto.Username);

            var apiUserModel = userCreateDto.Adapt<User>();
            apiUserModel.Password = protectedPassword;

            await _userRepository.CreateUser(apiUserModel);

            var tokenResponse = await _userRepository.AuthorizeWithPasswordGetToken(apiUserModel, userCreateDto.Password);
            var response = new
            {
                info = "user has been created successfully",
            };

            if (tokenResponse != null)
            {
                CreateSessionCookie(tokenResponse);
            }
            return Ok(response);
        }

        private void CreateSessionCookie(TokenModel tokenModel)
        {
            var cookieOptions = new CookieOptions()
            {
                Path = "/",
                IsEssential = true,
                HttpOnly = false,
                Secure = false
            };
            HttpContext.Response.Cookies.Append("session", tokenModel.AccessToken, cookieOptions);
            HttpContext.Response.Cookies.Append("refresh_session", tokenModel.RefreshToken, cookieOptions);
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login(UserCreateDto userCreateDto)
        {
            var inputPassword = userCreateDto.Password;

            var foundUser = await _userRepository.GetUserByName(userCreateDto.Username);

            if (foundUser == null)
            {
                return Ok("User not found");
            }
            var tokenResponse = await _userRepository.AuthorizeWithPasswordGetToken(foundUser, inputPassword);

            if (tokenResponse == null)
            {
                return Ok("Password not validated :(");
            }

            CreateSessionCookie(tokenResponse);
            return Ok($"Success logging in, access token: {tokenResponse.AccessToken}");
        }


        //[HttpPost("refresh")]
        //public async Task<ActionResult> Refresh([FromBody] TokenModel tokenModel)
        //{
        //    if (tokenModel == null)
        //    {
        //        return BadRequest("must provide access and refresh token");
        //    }

        //    string accessToken = tokenModel.AccessToken;
        //    string refreshToken = tokenModel.RefreshToken;

        //    var principal = _tokenizationService.GetPrincipalFromExpiredToken(accessToken);

        //    if (principal == null)
        //    {
        //        return BadRequest("Refresh token not validated");
        //    }

        //    var username = principal.Identity.Name;

        //    var user = await _userRepository.GetUserByName(username);
        //    if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiration <= DateTime.UtcNow)
        //    {
        //        return BadRequest("Invalid client credentials: bad refresh token or user not found");
        //    }

        //    var identity = _userRepository.GetIdentity(user);
        //    var newAccessToken = _tokenizationService.GenerateAccessToken(identity, JWTTokenTypes.Authorization);
        //    var newRefreshToken = _tokenizationService.GenerateRefreshToken();

        //    await _userRepository.SetRefreshTokenForUser(
        //        name: user.Username,
        //        refreshToken: newRefreshToken,
        //        refreshTokenExpiration: DateTime.UtcNow.AddDays(1));

        //    return new ObjectResult(new
        //    {
        //        accessToken = newAccessToken,
        //        refreshToken = newRefreshToken
        //    });
        //}

        //[Authorize]
        //[HttpPost("revoke")]
        //public async Task<IActionResult> Revoke()
        //{
        //    var username = User.Identity.Name;

        //    await _userRepository.RevokeRefreshTokenForUser(username); //revoke token
        //    return NoContent();
        //}

        [Authorize]
        [HttpGet("secret")]
        public ActionResult Secret()
        {
            return Ok($"Your login: {User.Identity.Name}");
        }
    }
}

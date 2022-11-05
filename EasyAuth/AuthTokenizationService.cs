using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EasyAuth
{
    public enum JWTTokenTypes
    {
        EmailVerification,
        Authorization
    }
    public class AuthTokenizationService : ITokenizationService
    {
        private readonly JwtLibOptions _options;
        public AuthTokenizationService(IOptionsMonitor<JwtLibOptions> opts)
        {
            _options = opts.CurrentValue;
        }

        public JwtLibOptions GetOptions()
        {
            return _options;
        }
        
        public string GenerateAccessToken(ClaimsIdentity identity)
        {

            var jwtAccessTokenObj = new JwtSecurityToken(
            issuer: _options.Authority,
            audience: _options.Audience,
            notBefore: DateTime.UtcNow,
            claims: identity.Claims,
            expires: DateTime.UtcNow.AddMinutes(_options.AuthTokenExpirationMinutes),
            signingCredentials: new SigningCredentials(GetSymmetricSecurityKey(_options.AuthKey),
            SecurityAlgorithms.HmacSha256)
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtAccessTokenObj);
            return accessToken;
        }

        public static ClaimsIdentity GetIdentity(string username) //used for jwt gen
        {

            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username),
                };

            ClaimsIdentity claimsIdentity = new(
                claims: claims,
                authenticationType: "Login",
                nameType: ClaimTypes.Name,
                null
            );
            return claimsIdentity;
        }

        public static long GetTokenExpirationTime(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = handler.ReadJwtToken(token);
            var tokenExp = jwtSecurityToken.Claims.First(claim => claim.Type.Equals("exp")).Value;
            var ticks = long.Parse(tokenExp);
            return ticks;
        }

        public static bool CheckAccessTokenIsValid(string token)
        {
            var tokenTicks = GetTokenExpirationTime(token);
            var tokenDate = DateTimeOffset.FromUnixTimeSeconds(tokenTicks).UtcDateTime;

            var now = DateTime.Now.ToUniversalTime();

            var valid = tokenDate >= now;

            return valid;
        }

        public ClaimsPrincipal GetPrincipalCheckAccessTokenNoLifetime(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false, //not checking token lifetime because if its an expired token, this will fail
                IssuerSigningKey = GetSymmetricSecurityKey(_options.AuthKey),
                ValidateIssuerSigningKey = true,
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;

            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
            JwtSecurityToken jwtSecurityToken = (JwtSecurityToken)securityToken;

            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            return principal;
        }

        public string GenerateRefreshToken()
        {
            var refreshToken = new byte[32];

            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(refreshToken);
            return Convert.ToBase64String(refreshToken);
        }

        public static SymmetricSecurityKey GetSymmetricSecurityKey(string key)
        {
            return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key));
        }

        public static HttpContext AppendCookiesToResponse(TokenModel tokens, HttpContext httpContext)
        {
            var cookieOptions = new CookieOptions()
            {
                Path = "/",
                IsEssential = true,
                HttpOnly = false,
                Secure = false
            };

            httpContext.Response.Cookies.Append("session", tokens.AccessToken, cookieOptions);
            httpContext.Response.Cookies.Append("refresh_session", tokens.RefreshToken, cookieOptions);
            return httpContext;
        }

        public static bool CheckTokenLifetimeIsValid(DateTime? expiryDateTime)
        {
            if (expiryDateTime != null)
            {
                return expiryDateTime >= DateTime.UtcNow;
            }
            return false;
        }

        public static HttpContext AppendTokensToResponseHeaders(TokenModel tokens, HttpContext httpContext)
        {
            httpContext.Response.Headers["Authorization"] = $"Bearer {tokens.AccessToken}";
            httpContext.Response.Headers["Authorization-Refresh"] = $"{tokens.RefreshToken}";
            return httpContext;
        }

        public static HttpContext RewriteAccessTokenHeader(string accessToken, HttpContext httpContext)
        {
            httpContext.Request.Headers["Authorization"] = $"Bearer {accessToken}";
            return httpContext;
        }

        public static TokenModel GetCookiesFromRequest(HttpContext httpContext)
        {
            var tokens = new TokenModel() {
                AccessToken = httpContext.Request.Cookies["session"],
                RefreshToken = httpContext.Request.Cookies["refresh_session"]
            };
            return tokens;
        }

        public static bool LifetimeValidator(DateTime? notBefore, DateTime? expires, SecurityToken token, TokenValidationParameters @params)
        {
            var isValid = CheckTokenLifetimeIsValid(expires);
            return isValid;
        }
    }
}

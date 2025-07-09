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

    public class AuthTokenizationService
    {
        private readonly JwtLibOptions _options;
        private readonly string _authority;
        private readonly string _audience;
        private readonly int _authTokenExpirationMinutes;
        private readonly string _authKey;
        private readonly bool _validateIssuer;
        private readonly bool _validateAudience;
        private readonly bool _validateLifetime;
        private readonly bool _validateIssuerSigningKey;
        private readonly bool _cookieHttpOnly;
        private readonly bool _cookieSecure;
        public AuthTokenizationService(
            IOptionsMonitor<JwtLibOptions> opts,
            IConfiguration configuration)
        {
            var authConfigPart = configuration.GetSection("AuthSettings");
            _options = opts.CurrentValue;
            //Secrets

            _authKey = _options.SecretAuthKey;

            //Config
            _authority = authConfigPart["Authority"];
            _audience = authConfigPart["Audience"];
            _authTokenExpirationMinutes = authConfigPart.GetValue("AuthTokenExpirationMinutes", 5);

            _validateIssuer = bool.Parse(authConfigPart["ValidationParameters:ValidateIssuer"]);
            _validateAudience = bool.Parse(authConfigPart["ValidationParameters:ValidateAudience"]);
            _validateLifetime = bool.Parse(authConfigPart["ValidationParameters:ValidateLifetime"]);
            _validateIssuerSigningKey = bool.Parse(authConfigPart["ValidationParameters:ValidateIssuerSigningKey"]);

            //Cookie conf
            _cookieHttpOnly = authConfigPart.GetValue("CookieParameters:HttpOnly", false);
            _cookieSecure = authConfigPart.GetValue("CookieParameters:Secure", false);
              //      "CookieParameters": {
              //          "HttpOnly": "false",
              //"Secure":  "false"
        }

        public JwtLibOptions GetOptions()
        {
            return _options;
        }

        public string GenerateAccessToken(ClaimsIdentity identity)
        {
            //_configuration["AuthRefreshTokenExpirationMinutes"]

            var jwtAccessTokenObj = new JwtSecurityToken(
            issuer: _authority,
            audience: _audience,
            notBefore: DateTime.UtcNow,
            claims: identity.Claims,
            expires: DateTime.UtcNow.AddMinutes(_authTokenExpirationMinutes),
            signingCredentials: new SigningCredentials(GetSymmetricSecurityKey(_authKey),
            SecurityAlgorithms.HmacSha256)
            );

            var token = new JwtSecurityTokenHandler().WriteToken(jwtAccessTokenObj);
            return token;
        }

        public static ClaimsIdentity GetIdentity(string username, string email, bool isVerified = false, List<string> roles = null) //used for jwt gen
        {
            List<Claim> claims =
            [
                //new ("Verified", isVerified.ToString().ToLower()),
                new (ClaimTypes.Name, username),
                new (ClaimTypes.Email, email)
            ];

            if (roles != null)
            {
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }


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

        public static bool CheckTokenNotExpired(string token)
        {
            var tokenTicks = GetTokenExpirationTime(token);
            var tokenDate = DateTimeOffset.FromUnixTimeSeconds(tokenTicks).UtcDateTime;

            var now = DateTime.Now.ToUniversalTime();

            var valid = tokenDate >= now;

            return valid;
        }

        public ClaimsPrincipal ValidateTokenGetPrincipal(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = _validateIssuer,
                ValidateAudience = _validateAudience,
                ValidateLifetime = _validateLifetime, //Don't check for access token lifetime because if its an expired access token, this will fail. do check refresh token!
                IssuerSigningKey = GetSymmetricSecurityKey(_authKey),
                ValidateIssuerSigningKey = _validateIssuerSigningKey,  //was true
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

        public HttpContext AppendCookiesToResponse(TokenModel tokens, HttpContext httpContext)
        {
            var cookieOptions = new CookieOptions()
            {
                Path = "/",
                IsEssential = true,
                HttpOnly = _cookieHttpOnly,
                Secure = _cookieSecure
            };

            httpContext.Response.Cookies.Append("session", tokens.AccessToken, cookieOptions);
            httpContext.Response.Cookies.Append("refresh_session", tokens.RefreshToken, cookieOptions);
            return httpContext;
        }

        public static bool CheckTokenLifetimeIsValid(DateTime? notBefore, DateTime? expires, SecurityToken token, TokenValidationParameters @params)
        {
            if (expires != null)
            {
                return expires >= DateTime.UtcNow;
            }
            return false;
        }
    }
}

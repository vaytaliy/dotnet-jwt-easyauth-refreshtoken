using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace EasyAuth
{
    public class ModifyAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ISimpleAuthDataAccess _simpleAuthDataAccess;
        private readonly AuthTokenizationService _tokenizationService;

        public ModifyAuthMiddleware(RequestDelegate next
            , ISimpleAuthDataAccess simpleAuthDataAccess
            , AuthTokenizationService tokenizationService)
        {
            _next = next;
            _simpleAuthDataAccess = simpleAuthDataAccess;
            _tokenizationService = tokenizationService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var providedTokens = AuthTokenizationService.GetCookiesFromRequest(context);
            var idenitityName = context.User.Identity.Name;
            var username = _tokenizationService.GetPrincipalCheckAccessTokenNoLifetime(providedTokens.AccessToken)
                .Identity.Name;

            if (providedTokens.AccessToken != null && username != null)
            {
                var userData = await _simpleAuthDataAccess.GetUser(username);
                var accessTokenIsValid = AuthTokenizationService.CheckAccessTokenIsValid(providedTokens.AccessToken);
                if (!accessTokenIsValid)
                {
                    var refreshTokenValid = AuthTokenizationService.CheckTokenLifetimeIsValid(userData.RefreshTokenExpiration);
                    if (refreshTokenValid && userData.RefreshToken == providedTokens.RefreshToken)
                    {
                        context = await ProvisionTokens(context, username);
                    }
                    else
                    {
                        context.Response.Headers["www-authenticate-refresh"] = $"Refresh token has expired at {userData.RefreshTokenExpiration} UTC";
                    }
                }
            }
            await _next(context);
        }

        private async Task<HttpContext> ProvisionTokens(HttpContext context, string username)
        {
            var tokenOptions = _tokenizationService.GetOptions();
            var newAccessToken = _tokenizationService.GenerateAccessToken(AuthTokenizationService.GetIdentity(username));
            var newRefreshToken = _tokenizationService.GenerateRefreshToken();
            await _simpleAuthDataAccess.PersistNewRefreshToken(username, newRefreshToken);
            var newTokens = new TokenModel()
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };

            context = AuthTokenizationService.RewriteAccessTokenHeader(newTokens.AccessToken, context);

            if (tokenOptions.SendTokensInResponseHeader == true)
            {
                context = AuthTokenizationService.AppendTokensToResponseHeaders(newTokens, context);
            }

            if (tokenOptions.SendCookies == true)
            {
                context = AuthTokenizationService.AppendCookiesToResponse(newTokens, context);
            }
            return context;
        }
    }
}

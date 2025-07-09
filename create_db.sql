--Only uncomment when certain

--/*
USE master; -- Switch to the master database
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = 'AuthDb')
BEGIN
    ALTER DATABASE AuthDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE AuthDb;
END
GO
--*/

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'AuthDb')
BEGIN
    CREATE DATABASE AuthDb;
END
GO

USE AuthDb;
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Users')
BEGIN
    CREATE TABLE [User]
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL,
        Password NVARCHAR(100) NOT NULL,
        Email NVARCHAR(100) NOT NULL,
        IsVerified BIT NOT NULL DEFAULT 0,
       -- RefreshToken NVARCHAR(100),
       -- RefreshTokenExpirationUTC DATETIME DEFAULT GETDATE(),
       -- AssignedRoles NVARCHAR(MAX) DEFAULT ''
    );
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Roles')
BEGIN
    CREATE TABLE Roles
    (
        RoleName NVARCHAR(50) PRIMARY KEY,
        RoleDescription NVARCHAR(255)
    );
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'UsersAndRoles')
BEGIN
    CREATE TABLE UsersAndRoles
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
		UserId INT NOT NULL,
		RoleName NVARCHAR(50) NOT NULL,
        CONSTRAINT FK_UsersAndRoles_UserId FOREIGN KEY (UserId) REFERENCES Users(Id),
		CONSTRAINT FK_UsersAndRoles_RoleName FOREIGN KEY (RoleName) REFERENCES Roles(RoleName)
    );
END


"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJyb2xlX25hbWUjMSI6IjEiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiYWRtaW4iLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJhZG1pbnVrYXMiLCJuYmYiOjE3MDg2NTM0NjIsImV4cCI6MTcwODY1MzUyMiwiaXNzIjoiaHR0cHM6Ly9sb2NhbGhvc3Q6NDQzMzcvIiwiYXVkIjoiaHR0cHM6Ly9sb2NhbGhvc3Q6NDQzMzcvIn0.9GpZMRnuzikIgADAc-S5nVQYwp289VZgKq_2rOH-fZc"

"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJyb2xlX25hbWUjMSI6IjEiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiYWRtaW4iLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJhZG1pbnVrYXMiLCJuYmYiOjE3MDg2NTM0NjIsImV4cCI6MTcwODY1MzUyMiwiaXNzIjoiaHR0cHM6Ly9sb2NhbGhvc3Q6NDQzMzcvIiwiYXVkIjoiaHR0cHM6Ly9sb2NhbGhvc3Q6NDQzMzcvIn0.9GpZMRnuzikIgADAc-S5nVQYwp289VZgKq_2rOH-fZc"


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
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
        private readonly IConfiguration _configuration;

        public ModifyAuthMiddleware(RequestDelegate next
            , ISimpleAuthDataAccess simpleAuthDataAccess
            , IConfiguration configuration
            , AuthTokenizationService tokenizationService)
        {
            _next = next;
            _simpleAuthDataAccess = simpleAuthDataAccess;
            _tokenizationService = tokenizationService;
            _configuration = configuration;
        }


        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();

            if (endpoint == null)
            {
                await _next(context);
                return;
            }

            if (endpoint.Metadata?.GetMetadata<AuthorizeAttribute>() == null)
            {
                await _next(context);
                return;
            }

            //depending on setting if cookie, then from cookie. Otherwise from header. [TBD] Allow both 
            var providedTokens = GetTokensFromRequest(context);

            //var idenitityName = context.User.Identity.Name;
            if (providedTokens.AccessToken != null)
            {
                ClaimsPrincipal claimsPrincipalFromAccessToken = _tokenizationService.ValidateTokenGetPrincipal(providedTokens.AccessToken);
                var usernameFromAccessToken = claimsPrincipalFromAccessToken.Identity.Name;

                var emailFromAccessTokenClaims = claimsPrincipalFromAccessToken.Claims
                        .FirstOrDefault(c => c.Type == ClaimTypes.Email).Value;

                var rolesFromAccessTokenClaims = claimsPrincipalFromAccessToken.Claims
                        .Where(c => c.Type.StartsWith("role_name#"))
                        .Select(c => c.Value)
                        .ToList();

                if (usernameFromAccessToken == null)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Response.ContentType = "text/plain";

                    await context.Response.WriteAsync("The access token is invalid");
                    return;
                }

                ClaimsPrincipal claimsPrincipalFromRefreshToken = _tokenizationService.ValidateTokenGetPrincipal(providedTokens.RefreshToken);

                var usernameFromRefreshToken = claimsPrincipalFromRefreshToken.Identity.Name;
                

                //var userData = await _simpleAuthDataAccess.GetUser(usernameFromAccessToken);
                var accessTokenIsValid = AuthTokenizationService.CheckTokenNotExpired(providedTokens.AccessToken);
                

                if (!accessTokenIsValid)
                {
                    var refreshTokenValid = AuthTokenizationService.CheckTokenNotExpired(providedTokens.RefreshToken);

                    if (refreshTokenValid)
                    {
                        context = ProvisionTokens(context, usernameFromAccessToken, emailFromAccessTokenClaims, rolesFromAccessTokenClaims);
                    }
                    else
                    {
                        var refreshTokenExpiredTimeUTC = DateTimeOffset.FromUnixTimeSeconds(AuthTokenizationService.GetTokenExpirationTime(providedTokens.RefreshToken)).UtcDateTime;

                        context.Response.Headers["www-authenticate-refresh"] = $"Refresh token has expired at {refreshTokenExpiredTimeUTC} UTC";
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        context.Response.ContentType = "text/plain";

                        await context.Response.WriteAsync("The access token is invalid");
                        return;

                    }
                }

                //var attribute = endpoint?.Metadata.GetMetadata<AllowedRolesAttribute>();
                //if (attribute != null)
                //{
                //    var roleList = attribute.RoleList;

                //    if (tst == roleList)
                //    {
                //        await _next(context);
                //    }

                //    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                //    context.Response.ContentType = "text/plain";

                //    await context.Response.WriteAsync("You don't have sufficient role to access this resource");
                //    return;
                //    // add telemetry or logging here
                //}

            }
            //return _next;
            await _next(context);
        }

        private TokenModel GetTokensFromRequest(HttpContext context)
        {
            if (_configuration["AuthSettings:UseCookies"] == "true")
            {
                return AuthTokenizationService.GetTokensFromCookies(context);
            }

            return AuthTokenizationService.GetTokensFromHeaders(context);
        }



        private HttpContext ProvisionTokens(HttpContext context, string username, string email, List<string> roleNames)
        {
            var tokenOptions = _tokenizationService.GetOptions();
            var newAccessToken = _tokenizationService.GenerateToken(
                    identity: AuthTokenizationService.GetIdentity(username, email, roleNames),
                    tokenExpirationMinutes: int.Parse(_configuration["AuthSettings:AuthTokenExpirationMinutes"]),
                    secretKey: _configuration["SecretAuthKey"]
                );
            var newRefreshToken = _tokenizationService.GenerateToken(
                    identity: AuthTokenizationService.GetIdentity(username, email, roleNames),
                    tokenExpirationMinutes: int.Parse(_configuration["AuthSettings:AuthRefreshTokenExpirationMinutes"]),
                    secretKey: _configuration["SecretAuthKey"]
                );
            //await _simpleAuthDataAccess.PersistNewRefreshToken(username, newRefreshToken);
            var newTokens = new TokenModel()
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };

            context = AuthTokenizationService.RewriteAccessTokenHeader(newTokens.AccessToken, context);

            if (_configuration["AuthSettings:UseCookies"] != "true")
            {
                context = AuthTokenizationService.AppendTokensToResponseHeaders(newTokens, context);
            }

            if (_configuration["AuthSettings:UseCookies"] == "true")
            {
                context = AuthTokenizationService.AppendCookiesToResponse(newTokens, context);
            }
            return context;
        }
    }
}

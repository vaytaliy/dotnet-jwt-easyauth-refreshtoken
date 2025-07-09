using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

//using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EasyAuth
{
    public static class AuthenticationBuilderExtension
    {
        public static AuthenticationBuilder AddJWTEasyAuth(
            this AuthenticationBuilder builder, 
            IConfiguration configuration,
            Action<JwtLibOptions> opts
            )
        {
            var authConfigPart            = configuration.GetSection("AuthSettings");
            bool validateIssuer           = authConfigPart.GetValue("ValidationParameters:ValidateIssuer", true);
            bool validateAudience         = authConfigPart.GetValue("ValidationParameters:ValidateAudience", true);
            bool validateLifetime         = authConfigPart.GetValue("ValidationParameters:ValidateLifetime", true);
            bool validateIssuerSigningKey = authConfigPart.GetValue("ValidationParameters:ValidateIssuerSigningKey", true);

            JwtLibOptions libOpts = new();
            opts.Invoke(libOpts);

            builder.AddJwtBearer(opts =>
            {
                opts.IncludeErrorDetails = false;
                opts.RequireHttpsMetadata = false;
                opts.UseSecurityTokenValidators = false;
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = validateIssuer,
                    ValidateAudience = validateAudience,
                    ValidateLifetime = validateLifetime,
                    IssuerSigningKey = AuthTokenizationService.GetSymmetricSecurityKey(libOpts.SecretAuthKey),
                    ValidateIssuerSigningKey = validateIssuerSigningKey,
                    LifetimeValidator = AuthTokenizationService.CheckTokenLifetimeIsValid
                };
                opts.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        AuthenticationHeaderValue authStringVal;
                        AuthenticationHeaderValue.TryParse(context.Request.Headers.Authorization, out authStringVal);
                        string authStringValParam = null;
                        if (authStringVal != null) authStringValParam = authStringVal.Parameter;

                        context.Token = context.Request.Cookies["session"] ?? authStringValParam;

                        return Task.CompletedTask;
                    },

                    OnChallenge = context => {

                        context.HandleResponse();
                        //if (context.Response.HasStarted)
                        //{
                        //    var c = context; //debug
                        //    return Task.CompletedTask;
                        //}
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";

                        var res = new
                        {
                            status = "unauthorized",
                            message = "The request is unauthorized"
                        };

                        var result = JsonSerializer.Serialize(res);
                        return context.Response.WriteAsync(result);
                    },

                    OnForbidden = context => {

                        context.Response.StatusCode = 403;
                        context.Response.ContentType = "application/json";

                        var res = new
                        {
                            status = "unauthorized",
                            message = "Insufficient permissions to access requested resource"
                        };
                        var result = JsonSerializer.Serialize(res);

                        return context.Response.WriteAsync(result);
                    }
                };
            });
            return builder;
        }
    }
}

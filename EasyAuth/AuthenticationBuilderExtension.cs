using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyAuth
{
    public class TokenVerificationParameters
    {
        public string Audience { get; set; }
        public string Authority { get; set; }
        public string AuthKey { get; set; }
    }

    public static class AuthenticationBuilderExtension
    {
        public static AuthenticationBuilder AddJWTEasyAuth(this AuthenticationBuilder builder, Action<TokenVerificationParameters> options)
        {
            var libOpts = new TokenVerificationParameters();
            options.Invoke(libOpts);
            builder.AddJwtBearer(opts =>
            {
                opts.RequireHttpsMetadata = false;
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = libOpts.Authority,

                    ValidateAudience = true,
                    ValidAudience = libOpts.Audience,
                    ValidateLifetime = true,
                    LifetimeValidator = AuthTokenizationService.LifetimeValidator,

                    IssuerSigningKey = AuthTokenizationService.GetSymmetricSecurityKey(libOpts.AuthKey),
                    ValidateIssuerSigningKey = true,
                };
                opts.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies["session"] ?? context.Request.Headers["Authorization"];
                        return Task.CompletedTask;
                    }
                };
            });
            return builder;
        }
    }
}

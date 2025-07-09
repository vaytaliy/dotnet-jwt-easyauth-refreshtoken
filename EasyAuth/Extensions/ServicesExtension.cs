using EasyAuth.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyAuth
{
    public static class ServicesExtension
    {
        
        public static IServiceCollection AddAuthOptions(this IServiceCollection services, Action<JwtLibOptions> options)
        {
            services.AddOptions<JwtLibOptions>().Configure(options.Invoke);
            return services;
        }

        public static IServiceCollection AddSimpleAuthTokenProvider(this IServiceCollection services)
        {
            services.AddTransient<AuthTokenizationService>();
            services.AddTransient<EmailingService>();
            return services;
        }

        public static IServiceCollection AddAuthenticationController(this IServiceCollection services)
        {
            services.AddControllers()
                .AddApplicationPart(typeof(AuthController).Assembly);
            return services;
        }
    }
}

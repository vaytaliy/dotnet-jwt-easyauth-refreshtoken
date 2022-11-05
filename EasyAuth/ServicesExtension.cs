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
        public static IServiceCollection AddSimpleAuthTokenProvider(this IServiceCollection services, Action<JwtLibOptions> options)
        {
            services.AddOptions<JwtLibOptions>().Configure((o) =>
            {
                options.Invoke(o);
            });
            services.AddTransient<AuthTokenizationService>();
            return services;
        }
    }
}

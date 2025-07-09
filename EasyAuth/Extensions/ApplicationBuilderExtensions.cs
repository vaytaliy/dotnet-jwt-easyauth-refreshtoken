using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyAuth
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseAuthenticationController(this IApplicationBuilder app, string routePrefix)
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "authentication",
                    pattern: $"{routePrefix}/{{action}}",
                    defaults: new { controller = "Auth" }
                );
            });

            return app;
        }
    }
}

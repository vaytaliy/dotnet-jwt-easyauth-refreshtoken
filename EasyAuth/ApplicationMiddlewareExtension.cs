using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyAuth
{
    public static class ApplicationMiddlewareExtension
    {
        public static IApplicationBuilder UseTokenRefreshing(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ModifyAuthMiddleware>();
        }
    }
}

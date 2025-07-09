using EasyAuth.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EasyAuth
{
    //[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class VerifiedUserFilter : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userRepository = context.HttpContext.RequestServices.GetRequiredService<IAuthUserRepository>();
            var user = context.HttpContext.User;

            var foundUser = await userRepository.GetUserByName(user.Identity.Name);
            if (!foundUser.IsVerified)
            {
                context.Result = new JsonResult(new
                {
                    message = "User's email must be verified"
                })
                { StatusCode = 403 };
            }
        }
    }
}

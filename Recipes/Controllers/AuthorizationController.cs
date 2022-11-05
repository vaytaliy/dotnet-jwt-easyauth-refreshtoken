using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Recipes.Controllers
{
    public class AuthorizationController : Controller
    {
        public IActionResult Index() //login as another user, sign out, register, log in
        {
            return View();
        }

        [HttpGet]
        public IActionResult Secretushka()
        {
            var username = User.Identity.Name ?? "bitch";
            return View(model: username);
        }

        public IActionResult Register()
        {
            return View();
        }
    }
}

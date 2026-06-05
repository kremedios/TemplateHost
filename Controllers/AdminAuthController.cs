/**
We have:

✔ ONE login controller for ALL modules
✔ Supports multiple RCLs via module
✔ Supports returnUrl navigation
*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;

using AppContractsSCO.Services.Security;

namespace TemplateHost.Controllers
{
    public class AdminAuthController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ISecureConfig _secureConfig;

        public AdminAuthController(
            IConfiguration config,
            ISecureConfig secureConfig)
        {
            _config = config;
            _secureConfig = secureConfig;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string username,
            string password,
            string module,
            string returnUrl = null)
        {
            // STEP 1: load correct credentials based on module
            var encUser = _config[$"AdminAuth:{module}:Username"];
            var encPass = _config[$"AdminAuth:{module}:Password"];

            var validUser = _secureConfig.Decrypt(encUser);
            var validPass = _secureConfig.Decrypt(encPass);

            if (username != validUser || password != validPass)
            {
                ViewBag.Error = "Invalid credentials";
                return View();
            }

            // STEP 2: create claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, $"Admin:{module}")
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("Cookies", principal);

            // STEP 3: redirect logic
            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }
/*
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Login");
        }
*/
public async Task<IActionResult> Logout()
{
    string module = "";

    if (User.IsInRole("Admin:juderemedios"))
        module = "juderemedios";
    else if (User.IsInRole("Admin:keswick"))
        module = "keswick";

    await HttpContext.SignOutAsync("Cookies");

    return RedirectToAction("Login", new { module });
}

    }
}
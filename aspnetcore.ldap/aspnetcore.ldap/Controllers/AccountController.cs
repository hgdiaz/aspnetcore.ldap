using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using aspnetcore.ldap.Models;
using aspnetcore.ldap.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace aspnetcore.ldap.Controllers
{
    public class AccountController : Controller
    {
        private readonly Services.IAuthenticationService _authService;

        public AccountController(Services.IAuthenticationService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = _authService.Login(model.Username, model.Password);

                    // If the user is authenticated, store its claims to cookie
                    if (user != null)
                    {
                        var userClaims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, user.Username),
                            new Claim(ClaimTypes.Email, user.Email)
                        };

                        // Roles
                        foreach (var role in user.Roles)
                        {
                            userClaims.Add(new Claim(ClaimTypes.Role, role));
                        }

                        var principal = new ClaimsPrincipal(
                            new ClaimsIdentity(userClaims, _authService.GetType().Name)
                        );

                        await HttpContext.SignInAsync(
                          CookieAuthenticationDefaults.AuthenticationScheme,
                            principal,
                            new AuthenticationProperties
                            {
                                IsPersistent = model.RememberMe
                            }
                        );

                        return Redirect(Url.IsLocalUrl(model.ReturnUrl)
                            ? model.ReturnUrl
                            : "/");
                    }

                    ModelState.AddModelError("", @"Your username or password
                        is incorrect. Please try again.");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}

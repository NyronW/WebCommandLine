using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.MvcDemo.Services;

namespace WebApp.MvcDemo.Controllers;

public class AccountController : Controller
{
    private readonly InMemoryUserStore _userStore;

    public AccountController(InMemoryUserStore userStore)
    {
        _userStore = userStore;
    }

    [HttpGet]
    public IActionResult Login(string returnUrl = "/")
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password, string returnUrl = "/")
    {
        if (_userStore.ValidateUser(username, password, out var principal))
        {
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return Redirect(returnUrl);
        }

        ModelState.AddModelError("", "Invalid username or password.");
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [Authorize(Policy = "AdminUser")]
    public IActionResult AdminPage()
    {
        return View();
    }

    [Authorize(Policy = "PowerUser")]
    public IActionResult PowerUserPage()
    {
        return View();
    }

    public IActionResult AccessDenied()
    {
        return View();
    }
}
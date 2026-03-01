using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Security.Infrastructure.Identity;
using Security.Web.Models.Account;

namespace Security.Web.Controllers;

public class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    ILogger<AccountController> logger) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("SuperAdmin") || User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var userName = model.Identifier;
        var atIndex = model.Identifier.IndexOf('@');
        if (atIndex > 0 && atIndex < model.Identifier.Length - 1)
        {
            var user = await userManager.FindByEmailAsync(model.Identifier);
            if (user != null)
                userName = user.UserName!;
        }

        var result = await signInManager.PasswordSignInAsync(
            userName, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            logger.LogInformation("User {Identifier} logged in.", model.Identifier);
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("User {Identifier} account locked out.", model.Identifier);
            return RedirectToAction(nameof(Lockout));
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        logger.LogInformation("User logged out.");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Lockout() => View();

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}

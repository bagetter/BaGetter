using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Authentication;
using BaGetter.Core.Configuration;
using BaGetter.Core.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BaGetter.Web.Pages;

public class LoginModel : PageModel
{
    private readonly IUserService _userService;
    private readonly IOptionsSnapshot<NugetAuthenticationOptions> _authOptions;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        IUserService userService,
        IOptionsSnapshot<NugetAuthenticationOptions> authOptions,
        ILogger<LoginModel> logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _authOptions = authOptions ?? throw new ArgumentNullException(nameof(authOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [BindProperty]
    [Required(ErrorMessage = "Username is required.")]
    public string Username { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [BindProperty(SupportsGet = true)]
    public string ReturnUrl { get; set; }

    public string ErrorMessage { get; set; }

    public bool IsEntraEnabled =>
        _authOptions.Value.Mode is AuthenticationMode.Entra or AuthenticationMode.Hybrid;

    public bool IsLocalEnabled =>
        _authOptions.Value.Mode is AuthenticationMode.Local or AuthenticationMode.Hybrid;

    [FromQuery(Name = "error")]
    public string ExternalError { get; set; }

    public IActionResult OnGet()
    {
        if (_authOptions.Value.Mode == AuthenticationMode.Config)
        {
            return RedirectToPage("/Index");
        }

        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Index");
        }

        if (!string.IsNullOrEmpty(ExternalError))
        {
            ErrorMessage = ExternalError;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (_authOptions.Value.Mode == AuthenticationMode.Config)
        {
            return RedirectToPage("/Index");
        }

        if (!IsLocalEnabled)
        {
            return RedirectToPage("/Index");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userService.FindByUsernameAsync(Username, cancellationToken);

        if (user == null || user.AuthProvider != AuthProvider.Local)
        {
            ErrorMessage = "Invalid username or password.";
            _logger.LogWarning("Login failed: user '{Username}' not found or not a local account", Username);
            return Page();
        }

        if (!user.IsEnabled)
        {
            ErrorMessage = "This account has been disabled.";
            _logger.LogWarning("Login failed: user '{Username}' is disabled", Username);
            return Page();
        }

        if (!user.CanLoginToUI)
        {
            ErrorMessage = "This account is not permitted to sign in to the web UI.";
            _logger.LogWarning("Login failed: user '{Username}' does not have UI login permission", Username);
            return Page();
        }

        if (await _userService.IsLockedOutAsync(user))
        {
            var minutesRemaining = user.LockedUntilUtc.HasValue
                ? (int)Math.Ceiling((user.LockedUntilUtc.Value - DateTime.UtcNow).TotalMinutes)
                : _authOptions.Value.LockoutMinutes;

            ErrorMessage = $"This account is locked due to too many failed attempts. Try again in {minutesRemaining} minute(s).";
            _logger.LogWarning("Login failed: user '{Username}' is locked out", Username);
            return Page();
        }

        var passwordValid = await _userService.VerifyPasswordAsync(user, Password);
        if (!passwordValid)
        {
            await _userService.RecordFailedLoginAsync(user.Id, cancellationToken);
            ErrorMessage = "Invalid username or password.";
            _logger.LogWarning("Login failed: invalid password for user '{Username}'", Username);
            return Page();
        }

        await _userService.ResetFailedLoginCountAsync(user.Id, cancellationToken);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("DisplayName", user.DisplayName ?? user.Username),
            new("AuthProvider", AuthProvider.Local.ToString()),
            new(Core.Authentication.AuthenticationConstants.IsAdminClaim, user.IsAdmin ? "true" : "false")
        };


        var identity = new ClaimsIdentity(claims, Core.Authentication.AuthenticationConstants.CookieScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            Core.Authentication.AuthenticationConstants.CookieScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                RedirectUri = ReturnUrl
            });

        _logger.LogInformation("User '{Username}' signed in successfully via local account", Username);

        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }

        return RedirectToPage("/Index");
    }

    public IActionResult OnGetEntraSignIn()
    {
        if (!IsEntraEnabled)
        {
            return RedirectToPage("/Index");
        }

        var redirectUrl = !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl)
            ? ReturnUrl
            : Url.Page("/Index");

        return Challenge(
            new AuthenticationProperties { RedirectUri = redirectUrl },
            Core.Authentication.AuthenticationConstants.EntraOidcScheme);
    }
}

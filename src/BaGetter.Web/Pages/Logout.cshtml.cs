using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace BaGetter.Web.Pages;

public class LogoutModel : PageModel
{
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(
        ILogger<LogoutModel> logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var username = User.Identity?.Name;

        await HttpContext.SignOutAsync(Core.Authentication.AuthenticationConstants.CookieScheme);

        _logger.LogInformation("User '{Username}' signed out", username);

        return RedirectToPage("/Index");
    }
}

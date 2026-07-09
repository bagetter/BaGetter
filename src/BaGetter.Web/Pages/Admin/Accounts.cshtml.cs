using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Authentication;
using BaGetter.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BaGetter.Web.Pages.Admin;

[Authorize(AuthenticationSchemes = Core.Authentication.AuthenticationConstants.CookieScheme)]
public class AccountsModel : PageModel
{
    private readonly IUserService _userService;
    private readonly IGroupService _groupService;

    public AccountsModel(IUserService userService, IGroupService groupService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _groupService = groupService ?? throw new ArgumentNullException(nameof(groupService));
    }

    public List<User> Users { get; set; } = new();
    public Dictionary<Guid, List<Group>> UserGroupMemberships { get; set; } = new();

    [BindProperty]
    [Required(ErrorMessage = "Username is required.")]
    [MaxLength(256)]
    public string NewUsername { get; set; }

    [BindProperty]
    [MaxLength(256)]
    public string NewDisplayName { get; set; }


    [BindProperty]
    [Required(ErrorMessage = "Password is required.")]
    [MinLength(12, ErrorMessage = "Password must be at least 12 characters.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; }

    [BindProperty]
    public bool NewCanLoginToUI { get; set; }

    public string SuccessMessage { get; set; }
    public string ErrorMessage { get; set; }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }

    private async Task<bool> IsCurrentUserAdminAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return false;
        return await _userService.IsAdminAsync(userId, cancellationToken);
    }

    private async Task LoadUsersAndGroupsAsync(CancellationToken cancellationToken)
    {
        Users = await _userService.GetAllUsersAsync(cancellationToken);
        var allGroups = await _groupService.GetAllGroupsAsync(cancellationToken);
        UserGroupMemberships = allGroups
            .Where(g => g.UserGroups != null)
            .SelectMany(g => g.UserGroups.Select(ug => new { ug.UserId, Group = g }))
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Group).ToList());
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
            return RedirectToPage("/Index");

        await LoadUsersAndGroupsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
            return RedirectToPage("/Index");

        if (!ModelState.IsValid)
        {
            await LoadUsersAndGroupsAsync(cancellationToken);
            return Page();
        }

        var existing = await _userService.FindByUsernameAsync(NewUsername, cancellationToken);
        if (existing != null)
        {
            ErrorMessage = $"Username '{NewUsername}' already exists.";
            await LoadUsersAndGroupsAsync(cancellationToken);
            return Page();
        }

        await _userService.CreateLocalUserAsync(
            NewUsername,
            NewDisplayName ?? NewUsername,
            NewPassword,
            NewCanLoginToUI,
            GetUserId(),
            cancellationToken);

        SuccessMessage = $"Account '{NewUsername}' created successfully.";
        await LoadUsersAndGroupsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostToggleEnabledAsync(
        Guid userId, bool isEnabled, CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
            return RedirectToPage("/Index");

        await _userService.SetEnabledAsync(userId, !isEnabled, cancellationToken);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleCanLoginToUIAsync(
        Guid userId, bool canLoginToUI, CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
            return RedirectToPage("/Index");

        await _userService.SetCanLoginToUIAsync(userId, !canLoginToUI, cancellationToken);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(
        Guid userId, string newPassword, CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
            return RedirectToPage("/Index");

        if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 12)
        {
            ErrorMessage = "Password must be at least 12 characters.";
            await LoadUsersAndGroupsAsync(cancellationToken);
            return Page();
        }

        await _userService.SetPasswordAsync(userId, newPassword, cancellationToken);

        SuccessMessage = "Password reset successfully.";
        await LoadUsersAndGroupsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
            return RedirectToPage("/Index");

        var user = await _userService.FindByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            ErrorMessage = "User not found.";
            await LoadUsersAndGroupsAsync(cancellationToken);
            return Page();
        }

        if (user.IsEnabled)
        {
            ErrorMessage = "Cannot delete an enabled account. Disable the account first.";
            await LoadUsersAndGroupsAsync(cancellationToken);
            return Page();
        }

        var username = user.Username;
        await _userService.DeleteUserAsync(userId, cancellationToken);

        SuccessMessage = $"Account '{username}' has been deleted.";
        await LoadUsersAndGroupsAsync(cancellationToken);
        return Page();
    }
}

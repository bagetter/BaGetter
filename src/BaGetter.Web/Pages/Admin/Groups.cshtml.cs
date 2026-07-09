using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Authentication;
using BaGetter.Core.Entities;
using BaGetter.Core.Feeds;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BaGetter.Web.Pages.Admin;

[Authorize(AuthenticationSchemes = Core.Authentication.AuthenticationConstants.CookieScheme)]
public class GroupsModel : PageModel
{
    private readonly IGroupService _groupService;
    private readonly IUserService _userService;
    private readonly IPermissionService _permissionService;
    private readonly IFeedService _feedService;

    public GroupsModel(
        IGroupService groupService,
        IUserService userService,
        IPermissionService permissionService,
        IFeedService feedService)
    {
        _groupService = groupService ?? throw new ArgumentNullException(nameof(groupService));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _feedService = feedService ?? throw new ArgumentNullException(nameof(feedService));
    }

    public List<Group> Groups { get; set; } = new();
    public List<User> AllUsers { get; set; } = new();
    public List<Feed> AllFeeds { get; set; } = new();
    public Dictionary<Guid, Dictionary<Guid, FeedPermission>> GroupPermissions { get; set; } = new();

    [FromQuery]
    public Guid? SavedGroupId { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Group name is required.")]
    [MaxLength(256)]
    public string NewGroupName { get; set; }

    [BindProperty]
    [MaxLength(128)]
    public string NewAppRoleValue { get; set; }

    [BindProperty]
    [MaxLength(4000)]
    public string NewDescription { get; set; }

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

    private async Task LoadGroupPermissionsAsync(CancellationToken cancellationToken)
    {
        AllFeeds = await _feedService.GetAllFeedsAsync(cancellationToken);

        var groupPermissions = await _permissionService.GetPermissionsByPrincipalTypeAsync(
            PrincipalType.Group, cancellationToken);

        GroupPermissions = new Dictionary<Guid, Dictionary<Guid, FeedPermission>>(Groups.Count);
        foreach (var group in Groups)
        {
            GroupPermissions[group.Id] = new Dictionary<Guid, FeedPermission>();
        }

        foreach (var permission in groupPermissions)
        {
            if (GroupPermissions.TryGetValue(permission.PrincipalId, out var perFeed))
            {
                perFeed[permission.FeedId] = permission;
            }
        }
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
            return RedirectToPage("/Index");

        Groups = await _groupService.GetAllGroupsAsync(cancellationToken);
        AllUsers = await _userService.GetAllUsersAsync(cancellationToken);
        await LoadGroupPermissionsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostCreateGroupAsync(CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
            return RedirectToPage("/Index");

        if (!ModelState.IsValid)
        {
            Groups = await _groupService.GetAllGroupsAsync(cancellationToken);
            AllUsers = await _userService.GetAllUsersAsync(cancellationToken);
            await LoadGroupPermissionsAsync(cancellationToken);
            return Page();
        }

        var existing = await _groupService.FindByNameAsync(NewGroupName, cancellationToken);
        if (existing != null)
        {
            ErrorMessage = $"Group '{NewGroupName}' already exists.";
            Groups = await _groupService.GetAllGroupsAsync(cancellationToken);
            AllUsers = await _userService.GetAllUsersAsync(cancellationToken);
            await LoadGroupPermissionsAsync(cancellationToken);
            return Page();
        }

        await _groupService.CreateGroupAsync(
            NewGroupName,
            string.IsNullOrWhiteSpace(NewAppRoleValue) ? null : NewAppRoleValue.Trim(),
            NewDescription,
            cancellationToken);

        SuccessMessage = $"Group '{NewGroupName}' created successfully.";
        Groups = await _groupService.GetAllGroupsAsync(cancellationToken);
        AllUsers = await _userService.GetAllUsersAsync(cancellationToken);
        await LoadGroupPermissionsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAddUserAsync(
        Guid groupId, Guid userId, CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
            return RedirectToPage("/Index");

        if (!await _groupService.CanManuallyModifyMembershipAsync(groupId, userId, cancellationToken))
        {
            ErrorMessage = "Cannot manually add Entra users to role-linked groups. Membership is managed by Azure AD App Roles.";
            Groups = await _groupService.GetAllGroupsAsync(cancellationToken);
            AllUsers = await _userService.GetAllUsersAsync(cancellationToken);
            await LoadGroupPermissionsAsync(cancellationToken);
            return Page();
        }

        await _groupService.AddUserToGroupAsync(userId, groupId, cancellationToken);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveUserAsync(
        Guid groupId, Guid userId, CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
            return RedirectToPage("/Index");

        if (!await _groupService.CanManuallyModifyMembershipAsync(groupId, userId, cancellationToken))
        {
            ErrorMessage = "Cannot manually remove Entra users from role-linked groups. Membership is managed by Azure AD App Roles.";
            Groups = await _groupService.GetAllGroupsAsync(cancellationToken);
            AllUsers = await _userService.GetAllUsersAsync(cancellationToken);
            await LoadGroupPermissionsAsync(cancellationToken);
            return Page();
        }

        await _groupService.RemoveUserFromGroupAsync(userId, groupId, cancellationToken);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSavePermissionsAsync(
        Guid groupId,
        List<FeedPermissionInput> permissions,
        CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
            return RedirectToPage("/Index");

        permissions ??= new List<FeedPermissionInput>();
        foreach (var permission in permissions)
        {
            if (permission.FeedId == Guid.Empty)
                continue;

            // Unchecking every permission revokes the row so we don't persist an
            // all-false row that has no effect.
            if (!permission.CanPush && !permission.CanPull && !permission.CanDelete)
            {
                var existing = await _permissionService.GetPermissionAsync(
                    groupId, PrincipalType.Group, permission.FeedId, cancellationToken);
                if (existing != null)
                {
                    await _permissionService.RevokePermissionAsync(existing.Id, cancellationToken);
                }
            }
            else
            {
                await _permissionService.GrantPermissionAsync(
                    groupId, PrincipalType.Group, permission.FeedId,
                    permission.CanPush, permission.CanPull, cancellationToken,
                    canDelete: permission.CanDelete);
            }
        }

        return RedirectToPage(new { savedGroupId = groupId });
    }

    public async Task<IActionResult> OnPostRevokePermissionAsync(
        Guid permissionId, CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
            return RedirectToPage("/Index");

        await _permissionService.RevokePermissionAsync(permissionId, cancellationToken);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteGroupAsync(
        Guid groupId, CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
            return RedirectToPage("/Index");

        await _groupService.DeleteGroupAsync(groupId, cancellationToken);
        SuccessMessage = "Group deleted successfully.";

        Groups = await _groupService.GetAllGroupsAsync(cancellationToken);
        AllUsers = await _userService.GetAllUsersAsync(cancellationToken);
        await LoadGroupPermissionsAsync(cancellationToken);
        return Page();
    }
}

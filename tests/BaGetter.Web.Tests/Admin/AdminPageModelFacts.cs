using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core;
using BaGetter.Web.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BaGetter.Web.Tests.Admin;

public sealed class AdminPageModelFacts
{
    [Fact]
    public async Task Manage_RedirectsToAdminIndex()
    {
        var auditLog = new Mock<IAdminAuditLog>();
        auditLog
            .Setup(log => log.WriteAsync(It.IsAny<AdminAuditEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var options = new Mock<IOptionsSnapshot<BaGetterOptions>>();
        options.Setup(snapshot => snapshot.Value).Returns(new BaGetterOptions());

        var model = new BaGetter.Web.Pages.Admin.IndexModel(
            Mock.Of<IPackageDatabase>(),
            Mock.Of<IAdminPackageService>(),
            auditLog.Object,
            options.Object,
            NullLogger<BaGetter.Web.Pages.Admin.IndexModel>.Instance)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };

        var result = await model.OnPostManageAsync(
            "unknown",
            "Example.Package",
            "1.0.0",
            null,
            null,
            "search",
            "listed",
            2,
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/Index", redirect.PageName);
        Assert.Equal("search", redirect.RouteValues["q"]);
        Assert.Equal("listed", redirect.RouteValues["status"]);
        Assert.Equal(2, redirect.RouteValues["page"]);
    }
}

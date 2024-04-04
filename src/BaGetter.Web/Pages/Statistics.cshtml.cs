using BaGetter.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace BaGetter.Web;

public class StatisticsModel : PageModel
{
    private readonly IOptionsSnapshot<StatisticsOptions> _options;

    public StatisticsModel(IOptionsSnapshot<StatisticsOptions> options)
    {
        _options = options;
    }

    public IActionResult OnGet()
    {
        if (!_options.Value.EnableStatisticsPage) return NotFound();

        return Page();
    }
}

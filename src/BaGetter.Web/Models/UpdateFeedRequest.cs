using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BaGetter.Web.Models;

public class UpdateFeedRequest
{
    [BindRequired]
    public string Name { get; set; }

    public string Description { get; set; }
}

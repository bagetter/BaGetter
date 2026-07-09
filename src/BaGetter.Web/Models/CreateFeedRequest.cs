using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BaGetter.Web.Models;

public class CreateFeedRequest
{
    [BindRequired]
    public string Slug { get; set; }

    [BindRequired]
    public string Name { get; set; }

    public string Description { get; set; }
}

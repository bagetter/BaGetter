using Microsoft.Extensions.Options;
using Xunit;

namespace BaGetter.Core.Tests;

public class RequestRateLimitOptionsValidationTests
{
    [Fact]
    public void Validate_ShouldFail_WhenPermitLimitIsLessThanOne()
    {
        var validator = new ValidateBaGetterOptions<RequestRateLimitOptions>(optionsName: null);
        var options = new RequestRateLimitOptions
        {
            PermitLimit = 0
        };

        var result = validator.Validate(Options.DefaultName, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains(nameof(RequestRateLimitOptions.PermitLimit)));
    }
}

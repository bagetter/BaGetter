using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BaGetter.Core;

public class StatisticsOptions : IValidatableObject
{
    public bool EnableStatisticsPage { get; set; } = true;

    public bool ListConfiguredServices { get; set; } = false;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        yield return ValidationResult.Success;
    }
}

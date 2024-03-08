using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BaGetter.Core;

public class HealthCheckOptions : IValidatableObject
{
    [Required]
    public string Path { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (! Path.StartsWith('/'))
        {
            yield return new ValidationResult(
                $"The {nameof(Path)} needs to start with a /",
                new[] { nameof(Path) });
        }
    }
}

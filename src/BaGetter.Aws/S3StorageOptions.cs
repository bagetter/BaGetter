using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BaGetter.Core;

namespace BaGetter.Aws;

public class S3StorageOptions : IValidatableObject
{
    [RequiredIf(nameof(SecretKey), null, IsInverted = true)]
    public string AccessKey { get; set; }

    [RequiredIf(nameof(AccessKey), null, IsInverted = true)]
    public string SecretKey { get; set; }

    [RequiredIf(nameof(Endpoint), null)]
    public string Region { get; set; }

    [RequiredIf(nameof(Region), null)]
    public Uri Endpoint { get; set; }

    [Required]
    public string Bucket { get; set; }

    public string Prefix { get; set; }

    public bool UseInstanceProfile { get; set; }

    public string AssumeRoleArn { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Endpoint != null && !string.IsNullOrEmpty(Region))
        {
            yield return new ValidationResult(
                $"Only one of S3 {nameof(Region)} or {nameof(Endpoint)} configuration can be set, but not both.",
                new[] { nameof(Region), nameof(Endpoint) });
        }

        if(Endpoint != null && !Endpoint.IsAbsoluteUri)
        {
            yield return new ValidationResult(
                $"The S3 {nameof(Endpoint)} must be an absolute URI.",
                new[] { nameof(Endpoint) });
        }
    }
}

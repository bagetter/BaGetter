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

    public bool ForcePathStyle { get; set; }

    /// <summary>
    /// Allows to use other AwsS3 providers without chuck encoding
    /// </summary>
    public bool UseChunkEncoding { get; set; } = true;

    [Required]
    public string Bucket { get; set; }

    public string Prefix { get; set; }

    public bool UseInstanceProfile { get; set; }

    public string AssumeRoleArn { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Endpoint is not null && !Endpoint.IsAbsoluteUri)
        {
            yield return new ValidationResult(
                $"The S3 {nameof(Endpoint)} must be an absolute URI.",
                new[] { nameof(Endpoint) });
        }
    }
}

using System;

namespace BaGetter.Core;

public class CorsPolicyOptions
{
    public bool AllowAnyOrigin { get; set; } = true;

    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

    public bool AllowAnyMethod { get; set; } = true;

    public string[] AllowedMethods { get; set; } = Array.Empty<string>();

    public bool AllowAnyHeader { get; set; } = true;

    public string[] AllowedHeaders { get; set; } = Array.Empty<string>();

    public bool AllowCredentials { get; set; } = false;
}

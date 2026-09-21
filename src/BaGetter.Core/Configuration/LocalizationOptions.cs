namespace BaGetter.Core.Configuration;

public class LocalizationOptions
{
    public static readonly string[] SupportedCultures = ["en-US", "is-IS", "da-DK", "sv-SE", "nb-NO", "pl-PL", "fi-FI", "es-ES", "pt-PT", "it-IT", "zh-CN", "fr-FR"];

    public bool Enabled { get; set; } = false;

    public string DefaultCulture { get; set; } = "en-US";
}

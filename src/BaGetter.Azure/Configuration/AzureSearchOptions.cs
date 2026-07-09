using System.ComponentModel.DataAnnotations;

namespace BaGetter.Azure.Configuration
{
    public class AzureSearchOptions
    {
        [Required]
        public string AccountName { get; set; }

        [Required]
        public string ApiKey { get; set; }
    }
}

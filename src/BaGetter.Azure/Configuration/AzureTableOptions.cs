using System.ComponentModel.DataAnnotations;

namespace BaGetter.Azure
{
    public class AzureTableOptions
    {
        [Required]
        public string ConnectionString { get; set; }
        public string TableName { get; set; } = "Packages";
    }
}

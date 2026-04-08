using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BaGetter.Azure
{
    public class AzureTableOptions : IValidatableObject
    {
        /// <summary>
        /// The Azure Table Storage connection string.
        /// If provided, takes precedence over <see cref="AccountName"/>.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// The Azure Storage account name. Used with managed identity
        /// (DefaultAzureCredential) when <see cref="ConnectionString"/> is not provided.
        /// </summary>
        public string AccountName { get; set; }

        public string TableName { get; set; } = "Packages";

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(ConnectionString) && string.IsNullOrEmpty(AccountName))
            {
                yield return new ValidationResult(
                    $"Either {nameof(ConnectionString)} or {nameof(AccountName)} must be configured.",
                    new[] { nameof(ConnectionString), nameof(AccountName) });
            }
        }
    }
}

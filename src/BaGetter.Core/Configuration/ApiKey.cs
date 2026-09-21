using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaGetter.Core.Configuration;
public class ApiKey
{
    public string Key { get; set; }

    /// <summary>
    /// Optional API key hash in format:
    /// PBKDF2$&lt;iterations&gt;$&lt;base64Salt&gt;$&lt;base64Hash&gt;
    /// If set, this is used instead of <see cref="Key"/>.
    /// </summary>
    public string KeyHash { get; set; }
}

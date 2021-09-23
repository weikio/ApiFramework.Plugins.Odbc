using System.Linq;
using System.Text.RegularExpressions;
using Weikio.ApiFramework.SDK.DatabasePlugin;

namespace Weikio.ApiFramework.Plugins.Odbc.Configuration
{
    public class OdbcOptions : DatabaseOptionsBase
    {
        public string TableColumnSelectQueryOverride { get; set; }
        public string Dialect { get; set; } = "sqlsrv";
    }
}

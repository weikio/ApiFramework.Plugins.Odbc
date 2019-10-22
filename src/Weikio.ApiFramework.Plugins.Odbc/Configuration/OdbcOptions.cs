using System;
using System.Linq;

namespace Weikio.ApiFramework.Plugins.Odbc.Configuration
{
    public class OdbcOptions
    {
        public string ConnectionString { get; set; }

        public string[] Tables { get; set; }

        public bool Includes(string tableName)
        {
            if (Tables?.Any() == false)
            {
                return true;
            }

            return Tables.Contains(tableName, StringComparer.OrdinalIgnoreCase);
        }

        public bool ShouldGenerateApisForTables()
        {
            if (Tables == null)
            {
                return true;
            }

            if (Tables.Length == 1 && Tables.First() == "")
            {
                return false;
            }

            return true;
        }
    }
}

using System.Data.Common;
using System.Data.Odbc;
using Weikio.ApiFramework.SDK.DatabasePlugin;

namespace Weikio.ApiFramework.Plugins.Odbc
{
    public class OdbcConnectionCreator : IConnectionCreator
    {
        private readonly DatabaseOptionsBase _configuration;

        public OdbcConnectionCreator(DatabaseOptionsBase configuration)
        {
            _configuration = configuration;
        }

        public DbConnection CreateConnection(DatabaseOptionsBase options)
        {
            var result = new OdbcConnection(options.ConnectionString);

            return result;
        }
    }
}

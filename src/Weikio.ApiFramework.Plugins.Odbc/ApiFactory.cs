using System;
using System.Collections.Generic;
using System.Security.Cryptography.Xml;
using Microsoft.Extensions.Logging;
using SqlKata.Compilers;
using Weikio.ApiFramework.Plugins.Odbc.Configuration;
using Weikio.ApiFramework.SDK.DatabasePlugin;

namespace Weikio.ApiFramework.Plugins.Odbc
{
    public class ApiFactory : DatabaseApiFactoryBase
    {
        public ApiFactory(ILogger<ApiFactory> logger, ILoggerFactory loggerFactory) : base(logger, loggerFactory)
        {
        }

        public List<Type> Create(OdbcOptions configuration)
        {
            Compiler compiler = null;

            if (string.Equals(configuration.Dialect, "mysql", StringComparison.InvariantCultureIgnoreCase))
            {
                compiler = new MySqlCompiler();
            }

            if (string.Equals(configuration.Dialect, "sqlsrv", StringComparison.InvariantCultureIgnoreCase))
            {
                compiler = new SqlServerCompiler() { UseLegacyPagination = true };
            }

            if (string.Equals(configuration.Dialect, "pervasive", StringComparison.InvariantCultureIgnoreCase))
            {
                compiler = new PervasiveCompiler() { UseLegacyPagination = true };
            }
            
            if (string.Equals(configuration.Dialect, "sqlite", StringComparison.InvariantCultureIgnoreCase))
            {
                compiler = new SqliteCompiler();
            }

            if (string.Equals(configuration.Dialect, "oracle", StringComparison.InvariantCultureIgnoreCase))
            {
                compiler = new OracleCompiler();
            }

            if (string.Equals(configuration.Dialect, "postgres", StringComparison.InvariantCultureIgnoreCase))
            {
                compiler = new PostgresCompiler();
            }

            if (string.Equals(configuration.Dialect, "firebird", StringComparison.InvariantCultureIgnoreCase))
            {
                compiler = new FirebirdCompiler();
            }

            if (compiler == null)
            {
                throw new Exception("Unknown dialect. Supported are: mysql, firebird, postgres, oracle, sqlite, sqlsrv, pervasive");
            }

            var tableColumnSelectQuery = GetTableColumnSelectQuery(configuration);
            
            var pluginSettings = new DatabasePluginSettings(config => new OdbcConnectionCreator(config),
                tableName => string.Format(tableColumnSelectQuery, tableName), compiler);

            pluginSettings.AdditionalReferences.Add(typeof(SqlKata.Column).Assembly);
            pluginSettings.AdditionalReferences.Add(typeof(Table).Assembly);

            var result = Generate(configuration, pluginSettings);

            return result;
        }

        protected string GetTableColumnSelectQuery(OdbcOptions configuration)
        {
            if (!string.IsNullOrWhiteSpace(configuration.TableColumnSelectQueryOverride))
            {
                return configuration.TableColumnSelectQueryOverride;
            }
            
            if (string.Equals(configuration.Dialect, "mysql", StringComparison.InvariantCultureIgnoreCase))
            {
                return "select * from {0} limit 1";
            }

            if (string.Equals(configuration.Dialect, "sqlsrv", StringComparison.InvariantCultureIgnoreCase))
            {
                return "select top 0 * from {0}";
            }
            
            if (string.Equals(configuration.Dialect, "pervasive", StringComparison.InvariantCultureIgnoreCase))
            {
                return "select top 0 * from {0}";
            }

            if (string.Equals(configuration.Dialect, "sqlite", StringComparison.InvariantCultureIgnoreCase))
            {
                return "select * from {0} limit 1";
            }

            if (string.Equals(configuration.Dialect, "oracle", StringComparison.InvariantCultureIgnoreCase))
            {
                return "select * from {0} FETCH NEXT 11 ROWS ONLY;";
            }

            if (string.Equals(configuration.Dialect, "postgres", StringComparison.InvariantCultureIgnoreCase))
            {
                return "select * from {0} limit 1";
            }

            if (string.Equals(configuration.Dialect, "firebird", StringComparison.InvariantCultureIgnoreCase))
            {
                return "select first 1 * from {0}";
            }

            throw new Exception("Unknown dialect");
        }
    }
}

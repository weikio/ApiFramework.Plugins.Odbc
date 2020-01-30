using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Weikio.ApiFramework.Plugins.Odbc.CodeGeneration;
using Weikio.ApiFramework.Plugins.Odbc.Configuration;
using Weikio.ApiFramework.Plugins.Odbc.Schema;

namespace Weikio.ApiFramework.Plugins.Odbc
{
    public static class ApiFactory
    {
        public static Task<IEnumerable<Type>> Create(OdbcOptions odbcOptions, SqlCommands sqlCommands = null)
        {
            var querySchema = new List<Table>();
            var nonQueryCommands = new SqlCommands();

            using (var schemaReader = new SchemaReader(odbcOptions))
            {
                schemaReader.Connect();

                if (sqlCommands != null)
                {
                    var commandsSchema = schemaReader.GetSchemaFor(sqlCommands);

                    querySchema.AddRange(commandsSchema.QueryCommands);
                    nonQueryCommands = commandsSchema.NonQueryCommands;
                }

                if (odbcOptions.ShouldGenerateApisForTables())
                {
                    var dbTables = schemaReader.ReadSchemaFromDatabaseTables();
                    querySchema.AddRange(dbTables);
                }
            }

            var generator = new CodeGenerator();
            var assembly = generator.GenerateAssembly(querySchema, nonQueryCommands, odbcOptions);

            var result = assembly.GetExportedTypes()
                .Where(x => x.Name.EndsWith("Api"))
                .ToList();

            return Task.FromResult<IEnumerable<Type>>(result);
        }
    }
}

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
        public static Task<IEnumerable<Type>> Create(OdbcOptions configuration)
        {
            var querySchema = new List<Table>();
            var nonQueryCommands = new SqlCommands();

            using (var schemaReader = new SchemaReader(configuration))
            {
                schemaReader.Connect();

                if (configuration.SqlCommands?.Any() == true)
                {
                    var commandsSchema = schemaReader.GetSchemaFor(configuration.SqlCommands);

                    querySchema.AddRange(commandsSchema.QueryCommands);
                    nonQueryCommands = commandsSchema.NonQueryCommands;
                }

                if (configuration.ShouldGenerateApisForTables())
                {
                    var dbTables = schemaReader.ReadSchemaFromDatabaseTables(); 
                    querySchema.AddRange(dbTables);
                }
            }

            var generator = new CodeGenerator();
            var assembly = generator.GenerateAssembly(querySchema, nonQueryCommands, configuration);

            var result = assembly.GetExportedTypes()
                .Where(x => x.Name.EndsWith("Api"))
                .ToList();

            return Task.FromResult<IEnumerable<Type>>(result);
        }
    }
}

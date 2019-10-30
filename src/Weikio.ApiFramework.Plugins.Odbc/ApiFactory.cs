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
            var schema = new List<Table>();

            using (var schemaReader = new SchemaReader(odbcOptions))
            {
                schemaReader.Connect();

                if (sqlCommands != null)
                {
                    schema.AddRange(schemaReader.GetSchemaFor(sqlCommands));
                }

                if (odbcOptions.ShouldGenerateApisForTables())
                {
                    schema.AddRange(schemaReader.ReadSchemaFromDatabaseTables());
                }
            }

            var generator = new CodeGenerator();
            var assembly = generator.GenerateAssembly(schema, odbcOptions);

            var result = assembly.GetExportedTypes()
                .Where(x => x.Name.EndsWith("Api"))
                .ToList();

            return Task.FromResult<IEnumerable<Type>>(result);
        }
    }
}

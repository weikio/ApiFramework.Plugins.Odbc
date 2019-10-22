using System;
using System.Collections.Generic;
using System.Linq;
using ApiFramework.Plugins.Odbc.CodeGeneration;
using ApiFramework.Plugins.Odbc.Configuration;
using ApiFramework.Plugins.Odbc.Schema;

namespace ApiFramework.Plugins.Odbc
{
    public static class FunctionFactory
    {
        public static IList<Type> Create(OdbcOptions odbcOptions, SqlCommands sqlCommands = null)
        {
            var schema = new List<Table>();

            using (var schemaReader = new SchemaReader(odbcOptions))
            {
                schemaReader.Connect();

                if (sqlCommands != null) schema.AddRange(schemaReader.GetSchemaFor(sqlCommands));

                if (odbcOptions.ShouldGenerateFunctionsForTables())
                    schema.AddRange(schemaReader.ReadSchemaFromDatabaseTables());
            }

            var generator = new CodeGenerator();
            var assembly = generator.GenerateAssembly(schema, odbcOptions);

            return assembly.GetExportedTypes()
                .Where(x => x.Name.EndsWith("Function"))
                .ToList();
        }
    }
}
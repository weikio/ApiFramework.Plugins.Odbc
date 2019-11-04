using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Reflection;
using LamarCodeGeneration;
using LamarCompiler;
using Weikio.ApiFramework.Plugins.Odbc.Configuration;
using Weikio.ApiFramework.Plugins.Odbc.Schema;

namespace Weikio.ApiFramework.Plugins.Odbc.CodeGeneration
{
    public class CodeGenerator
    {
        public Assembly GenerateAssembly(IList<Table> schema, OdbcOptions odbcOptions)
        {
            var generator = new AssemblyGenerator();
            generator.ReferenceAssembly(typeof(System.Console).Assembly);
            generator.ReferenceAssembly(typeof(System.Data.DataRow).Assembly);
            generator.ReferenceAssembly(typeof(System.Data.Odbc.OdbcCommand).Assembly);
            generator.ReferenceAssembly(typeof(OdbcHelpers).Assembly);

            var assemblyCode = GenerateCode(schema, odbcOptions);

            try
            {
                var result = generator.Generate(assemblyCode);

                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                throw;
            }
        }

        public string GenerateCode(IList<Table> schema, OdbcOptions odbcOptions)
        {
            using (var source = new SourceWriter())
            {
                source.UsingNamespace("System");
                source.UsingNamespace("System.Collections.Generic");
                source.UsingNamespace("System.Data");
                source.UsingNamespace("System.Data.Odbc");
                source.UsingNamespace("System.Reflection");
                source.UsingNamespace("System.Linq");
                source.UsingNamespace("System.Diagnostics");
                source.UsingNamespace("Weikio.ApiFramework.Plugins.Odbc.Configuration");
                source.UsingNamespace("Weikio.ApiFramework.Plugins.Odbc.Schema");
                source.WriteLine("");

                foreach (var table in schema)
                {
                    source.WriteNamespaceBlock(table, namespaceBlock =>
                    {
                        namespaceBlock.WriteDataTypeClass(table);

                        namespaceBlock.WriteApiClass(table, odbcOptions);
                    });
                }

                return source.Code();
            }
        }
    }
}

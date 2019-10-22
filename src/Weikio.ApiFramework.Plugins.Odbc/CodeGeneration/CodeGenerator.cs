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
            generator.ReferenceAssembly(typeof(Console).Assembly);
            generator.ReferenceAssembly(typeof(DataRow).Assembly);
            generator.ReferenceAssembly(typeof(OdbcCommand).Assembly);
            generator.ReferenceAssembly(typeof(OdbcHelpers).Assembly);

            var assemblyCode = GenerateCode(schema, odbcOptions);

            return generator.Generate(assemblyCode);
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

using System;
using SqlKata;
using SqlKata.Compilers;

namespace Weikio.ApiFramework.Plugins.Odbc
{
    public class PervasiveCompiler : SqlServerCompiler
    {
        public PervasiveCompiler()
        {
            OpeningIdentifier = "\"";
            ClosingIdentifier = "\"";
            parameterPrefix = "?";
        }
        
        public override SqlResult Compile(Query query)
        {
            var compiled = CompileRaw(query);

            compiled.NamedBindings = generateNamedBindings(compiled.Bindings.ToArray());
            compiled.Sql = Helper.ReplaceAll(compiled.RawSql, parameterPlaceholder, i => parameterPrefix);

            return compiled;
        }
    }
}

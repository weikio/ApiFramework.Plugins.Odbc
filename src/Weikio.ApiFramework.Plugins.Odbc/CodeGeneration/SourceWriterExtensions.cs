using System;
using System.Collections.Generic;
using System.Linq;
using LamarCodeGeneration;
using Microsoft.CodeAnalysis.CSharp;
using Weikio.ApiFramework.Plugins.Odbc.Configuration;
using Weikio.ApiFramework.Plugins.Odbc.Schema;

namespace Weikio.ApiFramework.Plugins.Odbc.CodeGeneration
{
    public static class SourceWriterExtensions
    {
        public static void WriteNamespaceBlock(this ISourceWriter writer, Table table,
            Action<ISourceWriter> contentProvider)
        {
            writer.Namespace(typeof(ApiFactory).Namespace + ".Generated" + table.Name);

            contentProvider.Invoke(writer);

            writer.FinishBlock(); // Finish the namespace
        }

        public static void WriteNamespaceBlock(this ISourceWriter writer, KeyValuePair<string, SqlCommand> command,
            Action<ISourceWriter> contentProvider)
        {
            writer.Namespace(typeof(ApiFactory).Namespace + ".Generated" + command.Key);

            contentProvider.Invoke(writer);

            writer.FinishBlock(); // Finish the namespace
        }

        public static void WriteDataTypeClass(this ISourceWriter writer, Table table)
        {
            writer.StartClass($"{GetDataTypeName(table)}");

            foreach (var column in table.Columns)
            {
                writer.WriteLine($"public {column.Type.NameInCode()} {GetPropertyName(column.Name)} {{ get;set; }}");
            }

            writer.WriteLine("");

            writer.Write("BLOCK:public object this[string propertyName]");
            writer.WriteLine("get{return this.GetType().GetProperty(propertyName).GetValue(this, null);}");
            writer.WriteLine("set{this.GetType().GetProperty(propertyName).SetValue(this, value, null);}");
            writer.FinishBlock(); // Finish the this-block

            writer.FinishBlock(); // Finish the class
        }

        public static void WriteQueryApiClass(this ISourceWriter writer, Table table, OdbcOptions odbcOptions)
        {
            writer.StartClass(GetApiClassName(table));

            writer.WriteLine($"private readonly ILogger<{GetApiClassName(table)}> _logger;");
            writer.WriteLine($"public {GetApiClassName(table)} (ILogger<{GetApiClassName(table)}> logger)");
            writer.WriteLine("{");
            writer.WriteLine("_logger = logger;");
            writer.WriteLine("}");

            var columnMap = new Dictionary<string, string>();

            foreach (var column in table.Columns)
            {
                columnMap.Add(column.Name, GetPropertyName(column.Name));
            }

            writer.Write("public static Dictionary<string, string> ColumnMap = new Dictionary<string, string>()");
            writer.Write("{");

            foreach (var columnPair in columnMap)
            {
                writer.Write($"    {{\"{columnPair.Key}\", \"{columnPair.Value}\"}},");
            }

            writer.WriteLine("};");
            writer.WriteLine("");

            writer.WriteLine("public OdbcOptions Configuration { get; set; }");

            if (table.SqlCommand != null)
            {
                writer.WriteCommandMethod(table.Name, table.SqlCommand, odbcOptions);
            }
            else
            {
                writer.WriteDefaultTableQueryMethod(table, odbcOptions);
            }

            writer.FinishBlock(); // Finish the class
        }

        public static void WriteNonQueryCommandApiClass(this ISourceWriter writer, KeyValuePair<string, SqlCommand> command, OdbcOptions odbcOptions)
        {
            writer.StartClass(GetApiClassName(command));

            writer.WriteLine("public OdbcOptions Configuration { get; set; }");

            writer.WriteCommandMethod(command.Key, command.Value, odbcOptions);

            writer.FinishBlock(); // Finish the class
        }

        private static void WriteCommandMethod(this ISourceWriter writer, string commandName, SqlCommand sqlCommand, OdbcOptions odbcOptions)
        {
            var sqlMethod = sqlCommand.CommandText.Trim()
                .Split(new[] { ' ' }, 2)
                .First().ToLower();
            sqlMethod = sqlMethod.Substring(0, 1).ToUpper() + sqlMethod.Substring(1);

            var methodParameters = new List<string>();

            if (sqlCommand.Parameters != null)
            {
                foreach (var sqlCommandParameter in sqlCommand.Parameters)
                {
                    var methodParam = "";

                    if (sqlCommandParameter.Optional)
                    {
                        var paramType = Type.GetType(sqlCommandParameter.Type);

                        if (paramType.IsValueType)
                        {
                            methodParam += $"{sqlCommandParameter.Type}? {sqlCommandParameter.Name} = null";
                        }
                        else
                        {
                            methodParam += $"{sqlCommandParameter.Type} {sqlCommandParameter.Name} = null";
                        }
                    }
                    else
                    {
                        methodParam += $"{sqlCommandParameter.Type} {sqlCommandParameter.Name}";
                    }

                    methodParameters.Add(methodParam);
                }
            }

            var dataTypeName = sqlCommand.IsNonQuery() ? "int" : GetDataTypeName(commandName, sqlCommand);
            var returnType = sqlCommand.IsNonQuery() ? "int" : $"IAsyncEnumerable<{dataTypeName}>";

            var cmdMethods = $"BLOCK:public async Task<{returnType}> {sqlMethod}({string.Join(", ", methodParameters)})";
            if (sqlCommand.IsQuery())
            {
                cmdMethods = $"BLOCK:public async {returnType} {sqlMethod}({string.Join(", ", methodParameters)})";
            }

            writer.Write(cmdMethods);

            if (sqlCommand.IsQuery() == false)
            {
                writer.WriteLine($"{returnType} result;");
            }

            writer.WriteLine("");

            writer.UsingBlock($"var conn = new OdbcConnection(\"{odbcOptions.ConnectionString}\")", w =>
            {
                w.WriteLine("await conn.OpenAsync();");

                w.UsingBlock("var cmd = conn.CreateCommand()", cmdBlock =>
                {
                    cmdBlock.WriteLine($"cmd.CommandText = @\"{sqlCommand.GetEscapedCommandText()}\";");

                    if (sqlCommand.Parameters != null)
                    {
                        foreach (var sqlCommandParameter in sqlCommand.Parameters)
                        {
                            cmdBlock.WriteLine(@$"OdbcHelpers.AddParameter(cmd, ""{sqlCommandParameter.Name}"", {sqlCommandParameter.Name});");
                        }
                    }

                    if (sqlCommand.IsQuery())
                    {
                        cmdBlock.UsingBlock("var reader = await cmd.ExecuteReaderAsync()", readerBlock =>
                        {
                            readerBlock.Write("BLOCK:while (await reader.ReadAsync())");
                            readerBlock.WriteLine($"var item = new {dataTypeName}();");
                            readerBlock.Write("BLOCK:foreach (var column in ColumnMap)");

                            readerBlock.Write(
                                "item[column.Value] = reader[column.Key] == DBNull.Value ? null : reader[column.Key];");
                            readerBlock.FinishBlock(); // Finish the column setting foreach loop

                            readerBlock.Write("yield return item;");
                            readerBlock.FinishBlock(); // Finish the while loop
                        });
                    }
                    else
                    {
                        cmdBlock.WriteLine("result = cmd.ExecuteNonQuery();");
                    }
                });
            });

            if (sqlCommand.IsQuery() == false)
            {
                writer.Write("return result;");
            }

            writer.FinishBlock(); // Finish the method
        }

        private static void WriteDefaultTableQueryMethod(this ISourceWriter writer, Table table,
            OdbcOptions odbcOptions)
        {
            var dataTypeName = GetDataTypeName(table);

            writer.Write($"BLOCK:public async IAsyncEnumerable<{dataTypeName}> Select(int? top)");
            writer.WriteLine($"var result = new List<{dataTypeName}>();");
            writer.WriteLine("var fields = new List<string>();");
            writer.WriteLine("");

            writer.UsingBlock($"var conn = new OdbcConnection(Configuration.ConnectionString)", w =>
            {
                w.WriteLine("await conn.OpenAsync();");

                w.UsingBlock("var cmd = conn.CreateCommand()", cmdBlock =>
                {
                    cmdBlock.WriteLine(
                        $"var queryAndParameters = OdbcHelpers.CreateQuery(\"{table.NameWithQualifier}\", top, fields);");
                    cmdBlock.WriteLine("var query = queryAndParameters.Item1;");
                    cmdBlock.WriteLine("cmd.Parameters.AddRange(queryAndParameters.Item2);");

                    cmdBlock.WriteLine("cmd.CommandText = query;");

                    cmdBlock.WriteLine("var sw = new System.Diagnostics.Stopwatch();");
                    cmdBlock.WriteLine("var rowcount = 0;");
                    cmdBlock.WriteLine("sw.Start();");

                    cmdBlock.UsingBlock("var reader = await cmd.ExecuteReaderAsync()", readerBlock =>
                    {
                        readerBlock.Write("BLOCK:while (reader.Read())");
                        readerBlock.WriteLine($"var item = new {dataTypeName}();");
                        readerBlock.WriteLine("var selectedColumns = ColumnMap;");
                        readerBlock.Write("BLOCK:if (fields?.Any() == true)");

                        readerBlock.Write(
                            "selectedColumns = ColumnMap.Where(x => fields.Contains(x.Key, StringComparer.OrdinalIgnoreCase)).ToDictionary(p => p.Key, p => p.Value);");
                        readerBlock.FinishBlock(); // Finish the if block
                        readerBlock.Write("BLOCK:foreach (var column in selectedColumns)");

                        readerBlock.Write(
                            "item[column.Value] = reader[column.Key] == DBNull.Value ? null : reader[column.Key];");
                        readerBlock.FinishBlock(); // Finish the column setting foreach loop

                        readerBlock.Write("yield return item;");
                        readerBlock.Write("rowcount += 1;");
                        readerBlock.FinishBlock(); // Finish the while loop
                    });

                    cmdBlock.WriteLine("sw.Stop();");
                    cmdBlock.WriteLine("_logger.LogTrace(\"Query took {ElapsedTime} and {RowCount} rows were found.\", sw.Elapsed, rowcount);");

                });
            });

            writer.FinishBlock(); // Finish the method
        }

        private static string GetApiClassName(Table table)
        {
            return $"{table.Name}Api";
        }

        private static string GetApiClassName(KeyValuePair<string, SqlCommand> command)
        {
            return $"{command.Key}Api";
        }

        private static string GetDataTypeName(Table table)
        {
            if (!string.IsNullOrEmpty(table.SqlCommand?.DataTypeName))
            {
                return table.SqlCommand.DataTypeName;
            }

            return table.Name + "Item";
        }

        private static string GetDataTypeName(string commandName, SqlCommand sqlCommand = null)
        {
            if (!string.IsNullOrEmpty(sqlCommand?.DataTypeName))
            {
                return sqlCommand.DataTypeName;
            }

            return commandName + "Item";
        }

        private static string GetPropertyName(string originalName)
        {
            var isValid = SyntaxFacts.IsValidIdentifier(originalName);

            if (isValid)
            {
                return originalName;
            }

            var result = originalName;

            if (result.Contains(" "))
            {
                result = result.Replace(" ", "").Trim();
            }

            if (SyntaxFacts.IsValidIdentifier(result))
            {
                return result;
            }

            return $"@{result}";
        }
    }
}

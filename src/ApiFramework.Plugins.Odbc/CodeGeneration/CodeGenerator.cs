﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using System.Reflection;
using ApiFramework.Plugins.Odbc.Configuration;
using ApiFramework.Plugins.Odbc.Schema;
using LamarCodeGeneration;
using LamarCompiler;
using Microsoft.CodeAnalysis.CSharp;

namespace ApiFramework.Plugins.Odbc.CodeGeneration
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
                    source.WriteNamespaceBlock(table, namespaceBlock =>
                    {
                        namespaceBlock.WriteDataTypeClass(table);

                        namespaceBlock.WriteFunctionClass(table, odbcOptions);
                    });

                return source.Code();
            }
        }
    }

    public static class SourceWriterExtensions
    {
        public static void WriteNamespaceBlock(this ISourceWriter writer, Table table,
            Action<ISourceWriter> contentProvider)
        {
            writer.Namespace(typeof(FunctionFactory).Namespace + ".Generated." + table.Name);

            contentProvider.Invoke(writer);

            writer.FinishBlock(); // Finish the namespace
        }

        public static void WriteDataTypeClass(this ISourceWriter writer, Table table)
        {
            writer.StartClass($"{GetDataTypeName(table)}");

            foreach (var column in table.Columns)
                writer.WriteLine($"public {column.Type.NameInCode()} {GetPropertyName(column.Name)} {{ get;set; }}");

            writer.WriteLine("");

            writer.Write("BLOCK:public object this[string propertyName]");
            writer.WriteLine("get{return this.GetType().GetProperty(propertyName).GetValue(this, null);}");
            writer.WriteLine("set{this.GetType().GetProperty(propertyName).SetValue(this, value, null);}");
            writer.FinishBlock(); // Finish the this-block

            writer.FinishBlock(); // Finish the class
        }

        public static void WriteFunctionClass(this ISourceWriter writer, Table table, OdbcOptions odbcOptions)
        {
            writer.StartClass(GetFunctionClassName(table));

            var columnMap = new Dictionary<string, string>();
            foreach (var column in table.Columns) columnMap.Add(column.Name, GetPropertyName(column.Name));

            writer.Write("public static Dictionary<string, string> ColumnMap = new Dictionary<string, string>()");
            writer.Write("{");
            foreach (var columnPair in columnMap)
                writer.Write($"    {{\"{columnPair.Key}\", \"{columnPair.Value}\"}},");

            writer.WriteLine("};");
            writer.WriteLine("");

            if (table.SqlCommand != null)
                writer.WriteSqlCommandMethod(table, odbcOptions);
            else
                writer.WriteDefaultTableQueryMethod(table, odbcOptions);

            writer.FinishBlock(); // Finish the class
        }

        private static void WriteSqlCommandMethod(this ISourceWriter writer, Table table, OdbcOptions odbcOptions)
        {
            var tableName = table.Name;
            var sqlCommand = table.SqlCommand;
            var sqlMethod = sqlCommand.CommandText.Trim()
                .Split(new[] {' '}, 2)
                .First().ToLower();
            sqlMethod = sqlMethod.Substring(0, 1).ToUpper() + sqlMethod.Substring(1);

            var methodParameters = new List<string>();
            if (sqlCommand.Parameters != null)
                foreach (var sqlCommandParameter in sqlCommand.Parameters)
                {
                    var methodParam = "";
                    if (sqlCommandParameter.Optional)
                    {
                        var paramType = Type.GetType(sqlCommandParameter.Type);
                        if (paramType.IsValueType)
                            methodParam += $"{sqlCommandParameter.Type}? {sqlCommandParameter.Name} = null";
                        else
                            methodParam += $"{sqlCommandParameter.Type} {sqlCommandParameter.Name} = null";
                    }
                    else
                    {
                        methodParam += $"{sqlCommandParameter.Type} {sqlCommandParameter.Name}";
                    }

                    methodParameters.Add(methodParam);
                }

            var dataTypeName = GetDataTypeName(table);

            writer.Write($"BLOCK:public List<{dataTypeName}> {sqlMethod}({string.Join(", ", methodParameters)})");
            writer.WriteLine($"var result = new List<{dataTypeName}>();");
            writer.WriteLine("");

            writer.UsingBlock($"var conn = new OdbcConnection(\"{odbcOptions.ConnectionString}\")", w =>
            {
                w.WriteLine("conn.Open();");
                w.UsingBlock("var cmd = conn.CreateCommand()", cmdBlock =>
                {
                    cmdBlock.WriteLine($"cmd.CommandText = @\"{sqlCommand.GetEscapedCommandText()}\";");

                    if (sqlCommand.Parameters != null)
                        foreach (var sqlCommandParameter in sqlCommand.Parameters)
                            cmdBlock.WriteLine(
                                $"cmd.Parameters.AddWithValue(\"@{sqlCommandParameter.Name}\", {sqlCommandParameter.Name});");

                    cmdBlock.UsingBlock("var reader = cmd.ExecuteReader()", readerBlock =>
                    {
                        readerBlock.Write("BLOCK:while (reader.Read())");
                        readerBlock.WriteLine($"var item = new {dataTypeName}();");
                        readerBlock.Write("BLOCK:foreach (var column in ColumnMap)");
                        readerBlock.Write(
                            "item[column.Value] = reader[column.Key] == DBNull.Value ? null : reader[column.Key];");
                        readerBlock.FinishBlock(); // Finish the column setting foreach loop

                        readerBlock.Write("result.Add(item);");
                        readerBlock.FinishBlock(); // Finish the while loop
                    });
                });
            });

            writer.Write("return result;");
            writer.FinishBlock(); // Finish the method
        }

        private static void WriteDefaultTableQueryMethod(this ISourceWriter writer, Table table,
            OdbcOptions odbcOptions)
        {
            var dataTypeName = GetDataTypeName(table);

            writer.Write($"BLOCK:public List<{dataTypeName}> Select(int? top)");
            writer.WriteLine($"var result = new List<{dataTypeName}>();");
            writer.WriteLine("var fields = new List<string>();");
            writer.WriteLine("");

            writer.UsingBlock($"var conn = new OdbcConnection(\"{odbcOptions.ConnectionString}\")", w =>
            {
                w.WriteLine("conn.Open();");
                w.UsingBlock("var cmd = conn.CreateCommand()", cmdBlock =>
                {
                    cmdBlock.WriteLine(
                        $"var queryAndParameters = OdbcHelpers.CreateQuery(\"{table.NameWithQualifier}\", top, fields);");
                    cmdBlock.WriteLine("var query = queryAndParameters.Item1;");
                    cmdBlock.WriteLine("cmd.Parameters.AddRange(queryAndParameters.Item2);");

                    cmdBlock.WriteLine("cmd.CommandText = query;");

                    cmdBlock.UsingBlock("var reader = cmd.ExecuteReader()", readerBlock =>
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

                        readerBlock.Write("result.Add(item);");
                        readerBlock.FinishBlock(); // Finish the while loop
                    });
                });
            });

            writer.Write("return result;");
            writer.FinishBlock(); // Finish the method
        }

        private static string GetFunctionClassName(Table table)
        {
            return $"{table.Name}Function";
        }

        private static string GetDataTypeName(Table table)
        {
            if (!string.IsNullOrEmpty(table.SqlCommand?.DataTypeName)) return table.SqlCommand.DataTypeName;

            return table.Name;
        }

        private static string GetPropertyName(string originalName)
        {
            var isValid = SyntaxFacts.IsValidIdentifier(originalName);
            if (isValid) return originalName;

            var result = originalName;
            if (result.Contains(" ")) result = result.Replace(" ", "").Trim();

            if (SyntaxFacts.IsValidIdentifier(result)) return result;

            return $"@{result}";
        }
    }
}
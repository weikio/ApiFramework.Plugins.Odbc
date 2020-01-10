using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text.RegularExpressions;

namespace Weikio.ApiFramework.Plugins.Odbc.Schema
{
    public static class OdbcHelpers
    {
        // Regex for finding an IN operator having just one parameter placeholder (?).
        // Ignores whitespace and is case insensitive.
        private static readonly Regex _inOperatorRegex = new Regex(@"\s+IN\s*\(\s*\?\s*\)", RegexOptions.IgnoreCase);

        public static (string, OdbcParameter[]) CreateQuery(string tableName, int? top, List<string> fields)
        {
            var sqlQuery =
                $"SELECT {(top.GetValueOrDefault() > 0 ? "TOP " + top.ToString() : "")} {(fields?.Any() == true ? string.Join(",", fields.Select(f => f.ToUpper())) : " * ")} FROM {tableName} ";

            return (sqlQuery, new OdbcParameter[] { });
        }

        public static void AddParameter(OdbcCommand odbcCommand, string name, object value)
        {
            if (value?.GetType().IsArray == true && _inOperatorRegex.IsMatch(odbcCommand.CommandText ?? ""))
            {
                var arrayValues = (object[])value;
                var arrayParameterNumber = 1;

                foreach (var parameterValue in arrayValues)
                {
                    var parameterName = $"@{name}_{arrayParameterNumber}";
                    odbcCommand.Parameters.AddWithValue(parameterName, parameterValue);

                    arrayParameterNumber += 1;
                }

                // Replace the single placeholder in IN function with the correct amount of placeholders.
                // For example: "STATUS IN (?)"  -->  "STATUS IN(?, ?, ?)"
                var parameterPlaceholders = string.Join(", ", Enumerable.Repeat("?", arrayValues.Length));
                odbcCommand.CommandText = _inOperatorRegex.Replace(odbcCommand.CommandText, $" IN({parameterPlaceholders})", count: 1);
            }
            else
            { 
                odbcCommand.Parameters.AddWithValue($"@{name}", value);
            }
        }
    }
}

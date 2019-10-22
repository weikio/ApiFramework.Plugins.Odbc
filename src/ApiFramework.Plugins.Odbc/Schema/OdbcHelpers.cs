using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;

namespace ApiFramework.Plugins.Odbc.Schema
{
    public static class OdbcHelpers
    {
        public static (string, OdbcParameter[]) CreateQuery(string tableName, int? top, List<string> fields)
        {
            var sqlQuery =
                $"SELECT {(top.GetValueOrDefault() > 0 ? "TOP " + top.ToString() : "")} {(fields?.Any() == true ? string.Join(",", fields.Select(f => f.ToUpper())) : " * ")} FROM {tableName} ";
            return (sqlQuery, new OdbcParameter[] { });
        }
    }
}
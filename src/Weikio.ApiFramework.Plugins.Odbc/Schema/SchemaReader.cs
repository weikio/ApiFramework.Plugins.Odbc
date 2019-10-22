using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using Weikio.ApiFramework.Plugins.Odbc.Configuration;

namespace Weikio.ApiFramework.Plugins.Odbc.Schema
{
    public class SchemaReader : IDisposable
    {
        private readonly OdbcOptions _options;

        private OdbcConnection _connection;

        public SchemaReader(OdbcOptions odbcOptions)
        {
            _options = odbcOptions;
        }

        public void Connect()
        {
            _connection = new OdbcConnection(_options.ConnectionString);
            _connection.Open();
        }

        private void RequireConnection()
        {
            if (_connection == null)
            {
                throw new InvalidOperationException("SchemaReader is not connected to a database.");
            }
        }

        public IList<Table> GetSchemaFor(SqlCommands sqlCommands)
        {
            RequireConnection();

            var schema = new List<Table>();

            if (sqlCommands?.Any() != true)
            {
                return schema;
            }

            foreach (var sqlCommand in sqlCommands)
            {
                using (var odbcCommand = _connection.CreateCommand())
                {
                    odbcCommand.CommandText = sqlCommand.Value.CommandText;

                    if (sqlCommand.Value.Parameters != null)
                    {
                        foreach (var parameter in sqlCommand.Value.Parameters)
                        {
                            var parameterType = Type.GetType(parameter.Type);

                            object parameterValue = null;

                            if (parameterType.IsValueType)
                            {
                                parameterValue = Activator.CreateInstance(parameterType);
                            }

                            odbcCommand.Parameters.AddWithValue(parameter.Name, parameterValue);
                        }
                    }

                    var columns = GetColumns(odbcCommand);
                    schema.Add(new Table($"{sqlCommand.Key}", "", columns, sqlCommand.Value));
                }
            }

            return schema;
        }

        public IList<Table> ReadSchemaFromDatabaseTables()
        {
            RequireConnection();

            var schema = new List<Table>();

            var schemaTables = _connection.GetSchema("Tables");

            foreach (DataRow schemaTable in schemaTables.Rows)
            {
                if (schemaTable["TABLE_TYPE"].ToString() != "TABLE")
                {
                    continue;
                }

                var tableQualifier = schemaTable["TABLE_QUALIFIER"].ToString();
                var tableName = schemaTable["TABLE_NAME"].ToString();
                var tableNameWithQualifier = $"{tableQualifier}.{tableName}";

                if (!_options.Includes(tableName))
                {
                    continue;
                }

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = $"select * from {tableNameWithQualifier}";
                    var columns = GetColumns(command);
                    schema.Add(new Table(tableName, tableQualifier, columns));
                }
            }

            return schema;
        }

        public IList<Column> GetColumns(OdbcCommand odbcCommand)
        {
            var columns = new List<Column>();

            using (var reader = odbcCommand.ExecuteReader())
            {
                using (var dtSchema = reader.GetSchemaTable())
                {
                    if (dtSchema != null)
                    {
                        foreach (DataRow schemaColumn in dtSchema.Rows)
                        {
                            var columnName = Convert.ToString(schemaColumn["ColumnName"]);
                            var dataType = (Type) schemaColumn["DataType"];
                            var isNullable = (bool) schemaColumn["AllowDBNull"];

                            columns.Add(new Column(columnName, dataType, isNullable));
                        }
                    }
                }
            }

            return columns;
        }

        #region IDisposable Support

        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_connection != null)
                    {
                        _connection.Dispose();
                        _connection = null;
                    }
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion
    }
}

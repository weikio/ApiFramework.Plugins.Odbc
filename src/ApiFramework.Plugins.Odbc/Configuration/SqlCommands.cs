using System.Collections.Generic;
using System.IO;

namespace ApiFramework.Plugins.Odbc.Configuration
{
    public class SqlCommands : Dictionary<string, SqlCommand>
    {
    }

    public class SqlCommand
    {
        private string _commandTextFile;
        public string CommandText { get; set; }

        public string CommandTextFile
        {
            get { return _commandTextFile; }
            set
            {
                _commandTextFile = value;

                if (!string.IsNullOrEmpty(_commandTextFile)) CommandText = File.ReadAllText(_commandTextFile);
            }
        }

        public string DataTypeName { get; set; }

        public SqlCommandParameter[] Parameters { get; set; }

        public string GetEscapedCommandText()
        {
            return CommandText.Replace("\"", "\"\"");
        }
    }

    public class SqlCommandParameter
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public bool Optional { get; set; }

        public object DefaultValue { get; set; } = null;
    }
}
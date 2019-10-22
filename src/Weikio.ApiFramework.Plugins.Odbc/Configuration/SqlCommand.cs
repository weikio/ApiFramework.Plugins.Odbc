using System.IO;

namespace Weikio.ApiFramework.Plugins.Odbc.Configuration
{
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

                if (!string.IsNullOrEmpty(_commandTextFile))
                {
                    CommandText = File.ReadAllText(_commandTextFile);
                }
            }
        }

        public string DataTypeName { get; set; }

        public SqlCommandParameter[] Parameters { get; set; }

        public string GetEscapedCommandText()
        {
            return CommandText.Replace("\"", "\"\"");
        }
    }
}

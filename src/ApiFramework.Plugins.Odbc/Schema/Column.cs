using System;

namespace ApiFramework.Plugins.Odbc.Schema
{
    public class Column
    {
        public Column(string name, Type type, bool isNullable)
        {
            Name = name;
            Type = type;
            IsNullable = isNullable;
        }

        public string Name { get; }

        public Type Type { get; }

        public bool IsNullable { get; }
    }
}
using System;

namespace TinyOrm.Abstraction.Attributes
{
    public class ColumnAttribute : Attribute
    {
        public ColumnAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
using System;

namespace TinyOrm.Abstraction.Attributes
{
    public class JoinTableAttribute : Attribute
    {
        public string Name;

        public JoinTableAttribute(string name)
        {
            Name = name;
        }
    }
}
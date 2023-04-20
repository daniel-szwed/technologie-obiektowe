using System;

namespace TinyOrm.Abstraction.Attributes
{
    public class ManyToManyAttribute : Attribute
    {
        public ManyToManyAttribute(string navigationProperty)
        {
            NavigationProperty = navigationProperty;
        }

        public string NavigationProperty { get; }
    }
}
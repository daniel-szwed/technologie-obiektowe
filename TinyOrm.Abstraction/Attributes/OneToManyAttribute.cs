using System;

namespace TinyOrm.Abstraction.Attributes
{
    public class OneToManyAttribute : Attribute
    {
        public OneToManyAttribute(string navigationProperty)
        {
            NavigationProperty = navigationProperty;
        }

        public string NavigationProperty { get; }
    }
}
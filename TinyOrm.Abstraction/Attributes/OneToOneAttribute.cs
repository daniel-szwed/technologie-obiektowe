using System;

namespace TinyOrm.Abstraction.Attributes
{
    public class OneToOneAttribute : Attribute
    {
        public OneToOneAttribute(string navigationProperty)
        {
            NavigationProperty = navigationProperty;
        }

        public string NavigationProperty { get; }
    }
}
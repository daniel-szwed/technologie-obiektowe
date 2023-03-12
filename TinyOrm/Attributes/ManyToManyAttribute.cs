namespace TinyOrm.Attributes
{
    internal class ManyToManyAttribute : Attribute
    {
        public string NavigationProperty { get; }

        public ManyToManyAttribute(string navigationProperty)
        {
            NavigationProperty = navigationProperty;
        }
    }
}
namespace TinyOrm.Attributes
{
    public class OneToOneAttribute : Attribute
    {
        public string NavigationProperty { get; }

        public OneToOneAttribute(string navigationProperty)
        {
            NavigationProperty = navigationProperty;
        }
    }
}
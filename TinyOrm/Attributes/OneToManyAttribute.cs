namespace TinyOrm.Attributes
{
    public class OneToManyAttribute : Attribute
    {
        public string NavigationProperty { get; set; }

        public OneToManyAttribute(string navigationProperty)
        {
            NavigationProperty = navigationProperty;
        }
    }
}
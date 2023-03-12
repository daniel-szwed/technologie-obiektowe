namespace TinyOrm.Attributes
{
    public class JoinTableAttribute : Attribute
    {
        public string Name;

        public JoinTableAttribute(string name)
        {
            this.Name = name;
        }
    }
}
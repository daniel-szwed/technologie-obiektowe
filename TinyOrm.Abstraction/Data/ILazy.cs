namespace TinyOrm.Abstraction.Data
{
    public interface ILazy
    {
        void SetProvider(IDataProvider provider);
    }
}
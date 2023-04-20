using System.Collections.Generic;

namespace TinyOrm.Abstraction.Data
{
    public interface IDataProvider
    {
        T CreateOrUpdate<T>(T entity) where T : EntityBase;
        T GetNestedEntity<T>(EntityBase parent, string nestedPropertyName);
        IEnumerable<T> ReadAll<T>() where T : EntityBase, new();
        bool Remove<T>(T entity) where T : EntityBase;
    }
}
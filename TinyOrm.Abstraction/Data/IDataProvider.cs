using System.Collections.Generic;

namespace TinyOrm.Abstraction.Data
{
    public interface IDataProvider
    {
        T CreateOrUpdate<T>(T entity) where T : EntityBase;
        T GetNestedEntity<T>(EntityBase parent, string nestedPropertyName);
        IEnumerable<T> ReadAll<T>() where T : EntityBase, new();
        T GetById<T>(long id) where T : EntityBase, new();
        bool Delete<T>(T entity) where T : EntityBase;
    }
}
using TinyOrm.Models;

namespace TinyOrm.DataProvider
{
    public interface IDataProvider
	{
		T CreateOrUpdate<T>(T entity) where T: EntityBase;
		IEnumerable<T> ReadAll<T>() where T: EntityBase, new();
		bool Remove<T>(T entity) where T : EntityBase;
	}
}


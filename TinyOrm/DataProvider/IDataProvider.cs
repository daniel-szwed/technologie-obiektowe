using TinyOrm.Models;

namespace TinyOrm.DataProvider
{
    public interface IDataProvider
	{
		bool CreateOrUpdate<T>(T entity);
		IEnumerable<T> ReadAll<T>() where T: EntityBase, new();
		bool Remove<T>(T entity);
	}
}


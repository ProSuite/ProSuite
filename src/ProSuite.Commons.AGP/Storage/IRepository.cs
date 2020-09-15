using System.Collections.Generic;

namespace ProSuite.Commons.AGP.Storage
{
	public interface IRepository<T> where T : class
	{
		IList<T> GetAll();
		void Add(T item);
		void Update(T item);
		void Delete(T item);
		void SaveChanges();
	}
}

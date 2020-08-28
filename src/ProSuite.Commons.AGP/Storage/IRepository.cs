using System.Collections.Generic;
using System.Linq;

namespace ProSuite.Commons.AGP.Storage
{
	public interface IRepository<T> where T : class
	{
		IQueryable<T> GetAll();
		void Add(T item);
		void Update(T item);
		void Delete(T item);
		void SaveChanges();
	}
}

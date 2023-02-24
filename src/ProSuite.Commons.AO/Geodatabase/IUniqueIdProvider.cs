using System.Collections.Generic;

namespace ProSuite.Commons.AO.Geodatabase
{
	public interface IUniqueIdProvider<T> : IUniqueIdProvider
	{
		long GetUniqueId(T feature);
	}

	public interface IUniqueIdProvider
	{
		bool Remove(long id);

		IList<int> GetOidFieldIndexes();
	}
}

using System.Collections.Generic;

namespace ProSuite.QA.Container.TestContainer
{
	public interface IUniqueIdProvider<T> : IUniqueIdProvider
	{
		long GetUniqueId(T feature);
	}

	public interface IUniqueIdProvider
	{
		bool Remove(long id);

		IList<InvolvedRow> GetInvolvedRows(long id);

		IList<int> GetOidFieldIndexes();
	}
}

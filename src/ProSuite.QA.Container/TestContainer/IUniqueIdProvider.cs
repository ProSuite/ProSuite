using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;

namespace ProSuite.QA.Container.TestContainer
{
	public interface IUniqueIdProvider<T> : IUniqueIdProvider
	{
		int GetUniqueId(T feature);
	}
	public interface IUniqueIdProvider
	{
		bool Remove(int id);

		IList<InvolvedRow> GetInvolvedRows(int id);

		IList<int> GetOidFieldIndexes();
	}
}

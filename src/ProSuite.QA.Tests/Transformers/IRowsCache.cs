using ProSuite.Commons.AO.Geodatabase;

namespace ProSuite.QA.Tests.Transformers
{
	public interface IRowsCache
	{
		bool Remove(long oid);

		void Add(IReadOnlyRow row);
	}
}

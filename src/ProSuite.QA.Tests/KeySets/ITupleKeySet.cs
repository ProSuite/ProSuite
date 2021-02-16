using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.KeySets
{
	public interface ITupleKeySet
	{
		bool Contains([NotNull] Tuple tuple);

		bool Add([NotNull] Tuple tuple);

		bool Remove([NotNull] Tuple tuple);

		int Count { get; }

		void Clear();
	}
}

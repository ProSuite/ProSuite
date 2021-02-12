using System.Collections;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.KeySets
{
	internal interface IKeySet : IEnumerable
	{
		bool Contains([NotNull] object key);

		bool Add([NotNull] object key);

		bool Remove([NotNull] object key);

		int Count { get; }

		void Clear();
	}
}

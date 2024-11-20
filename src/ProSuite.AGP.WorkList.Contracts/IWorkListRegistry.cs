using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface IWorkListRegistry
	{
		[CanBeNull]
		IWorkList Get([NotNull] string name);

		void Add([NotNull] IWorkList workList);

		void Add([NotNull] IWorkListFactory factory);

		bool TryAdd([NotNull] IWorkListFactory factory);

		bool Remove([NotNull] IWorkList workList);

		bool Remove([NotNull] string name);

		IEnumerable<string> GetNames();

		bool Contains([NotNull] string name);

		bool WorklistExists([NotNull] string name);

		bool AddOrReplace(IWorkList worklist);
	}
}

using System.Collections.Generic;
using System.Threading.Tasks;
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

		/// <summary>
		/// Whether the given name is a known work list. It could be registered only as
		/// XmlWorkListFactory or exist as an opened work list.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		bool Contains([NotNull] string name);

		/// <summary>
		/// Determines whether the work list of the given name as already be instantiated,
		/// i.e. opened in the navigator. If only the XmlWorkList factory has been registered,
		/// e.g. for display purposes, false is returned.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		bool WorklistExists([NotNull] string name);

		bool AddOrReplace(IWorkList worklist);

		Task<IWorkList> GetAsync(string name);
	}
}

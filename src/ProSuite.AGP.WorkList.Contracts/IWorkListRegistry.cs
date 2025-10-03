using System.Collections.Generic;
using System.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts
{
	// TODO: (daro) delete unused usages
	public interface IWorkListRegistry
	{
		IWorkList Get([NotNull] string name);

		bool TryAdd(IWorkList workList);

		bool TryAdd([NotNull] IWorkListFactory factory);

		bool Remove([NotNull] IWorkList workList);

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

		IAsyncEnumerable<IWorkList> GetAsync();

		IEnumerable<IWorkList> Get();

		void UnWire(string name);

		void UnWire(IWorkList workList);
	}
}

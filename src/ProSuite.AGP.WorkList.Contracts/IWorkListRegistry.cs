using System.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts;

public interface IWorkListRegistry
{
	[CanBeNull]
	IWorkList Get([NotNull] string name);

	/// <summary>
	/// Determines whether the work list of the given type has already been instantiated,
	/// i.e. opened in the navigator. If only the XmlWorkList factory has been registered,
	/// e.g. for display purposes, false is returned. Otherwise, true is returned
	/// </summary>
	/// <typeparam name="T">The work list type.</typeparam>
	/// <param name="name">The optional name of the work list. Only needed for work list types
	/// without single-instance semantics, such as selection work lists. </param>
	/// <param name="workList">The work list of the specified type, if it has been
	/// created</param>
	/// <returns></returns>
	bool TryGet<T>([CanBeNull] string name, out T workList) where T : class, IWorkList;

	bool TryAdd([NotNull] IWorkListFactory factory);

	bool AddOrReplace(IWorkList worklist);

	bool Remove([NotNull] IWorkList workList);

	/// <summary>
	/// Determines whether the work list of the given name has already been instantiated,
	/// i.e. opened in the navigator. If only the XmlWorkList factory has been registered,
	/// e.g. for display purposes, false is returned.
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	bool WorklistExists([NotNull] string name);

	Task<IWorkList> GetAsync(string name);
}

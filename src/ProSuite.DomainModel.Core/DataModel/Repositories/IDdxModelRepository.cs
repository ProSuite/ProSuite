using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel.Repositories
{
	/// <summary>
	/// Provides direct access to DdxModel for non-AO usages.
	/// </summary>
	public interface IDdxModelRepository // TODO Drop now that Model is AO-free (and dissolved into DdxModel)
	{
		/// <summary>
		/// Gets the model having the specified name.
		/// </summary>
		/// <param name="name">The name of the model.</param>
		/// <returns>the model for the given name, or null if no model found.</returns>
		[CanBeNull]
		DdxModel GetDdxModel([NotNull] string name);

		[NotNull]
		IList<DdxModel> GetAllDdxModels();
	}
}

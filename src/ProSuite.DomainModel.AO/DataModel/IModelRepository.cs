using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DomainModel.AO.DataModel
{
	public interface IModelRepository : IDdxModelRepository, IRepository<DdxModel>
	{
		/// <summary>
		/// Gets the model having the specified name.
		/// </summary>
		/// <param name="name">The name of the model.</param>
		/// <returns>the model for the given name, or null if no model found.</returns>
		[CanBeNull]
		DdxModel Get([NotNull] string name);
	}
}

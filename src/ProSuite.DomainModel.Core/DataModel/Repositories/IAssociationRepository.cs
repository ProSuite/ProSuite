using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel.Repositories
{
	public interface IAssociationRepository : IRepository<Association>
	{
		/// <summary>
		/// Gets the associations having the specified name.
		/// </summary>
		/// <param name="name">The fully qualified association name (schema.name).</param>
		/// <returns>the associations for the given name.</returns>
		[NotNull]
		IList<Association> Get([NotNull] string name);

		[NotNull]
		IList<Association> Get([NotNull] string name, bool includeDeleted);

		[NotNull]
		IList<T> GetAll<T>() where T : Association;

		/// <summary>
		/// Gets the association referencing a dataset with an id within the specified id set.
		/// </summary>
		/// <param name="datasetIds"></param>
		/// <returns></returns>
		[NotNull]
		IList<Association> GetByReferencedDatasetIds(IList<int> datasetIds);
	}
}

using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel.Repositories
{
	public interface IDatasetRepository : IRepository<Dataset>
	{
		/// <summary>
		/// Gets the datasets having the specified full name.
		/// </summary>
		/// <param name="name">The fully qualified dataset name (schema.table).</param>
		/// <returns>the datasets for the given name.</returns>
		[NotNull]
		IList<Dataset> Get([NotNull] string name);

		/// <summary>
		/// Gets the datasets having the specified full name, optionally including deleted datasets.
		/// </summary>
		/// <param name="name">The fully qualified dataset name (schema.table).</param>
		/// <param name="includeDeleted">if set to <c>true</c> deleted datasets are included. 
		/// Otherwise they are excluded from the result</param>
		/// <returns>
		/// the datasets for the given name.
		/// </returns>
		[NotNull]
		IList<Dataset> Get([NotNull] string name, bool includeDeleted);

		[CanBeNull]
		Dataset GetByAbbreviation([NotNull] DdxModel model, [NotNull] string abbreviation);

		[NotNull]
		IList<T> Get<T>([NotNull] DdxModel model) where T : Dataset;

		[NotNull]
		IList<T> GetAll<T>() where T : Dataset;

		[NotNull]
		IList<Dataset> Get([NotNull] DatasetCategory datasetCategory);
	}
}

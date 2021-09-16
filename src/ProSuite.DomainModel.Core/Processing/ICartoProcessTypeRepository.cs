using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.Processing
{
	public interface ICartoProcessTypeRepository : IRepository<CartoProcessType>
	{
		/// <summary>
		/// Gets the count of carto processes that are based on a <see cref="CartoProcessType"/>,
		/// as a map CartoProcessType.Name -> number of referencing carto processes, for all carto process types.
		/// Unreferenced carto process types are not contained in the map -> implied count is 0.
		/// </summary>
		/// <returns>dictionary [CartoProcessType.Name] -> [number of referencing quality conditions]</returns>
		[NotNull]
		IDictionary<string, int> GetReferencingCartoProcessesCount();
	}
}

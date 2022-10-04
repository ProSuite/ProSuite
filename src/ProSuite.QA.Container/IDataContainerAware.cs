using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	/// <summary>
	/// Somewhat compulsory interface to be implemented by transformed tables/feature classes.
	/// It is used to set the search container to be used for searching in the implementation of
	/// input (transformed) tables.
	/// The data container is assigned recursively to the input tables of implementors which could
	/// again be transformed tables with their own input tables etc.
	/// Therefore this interface must be implemented by transformed tables that can have potentially
	/// transformed input tables. Even if a transformed table does not use the data container,
	/// it's involved tables might be transformed tables that need the container.
	/// Additionally the input (involved) tables are also added to the cache, if they implement the
	/// additional interface ITransformedTable. Therefore all transformed tables must implement
	/// this interface if they can have input tables that potentially could be cached.
	/// </summary>
	public interface IDataContainerAware
	{
		/// <summary>
		/// The tables that are recursively checked if they implement <see cref="IDataContainerAware"/>
		/// as well because they (or their upstream transformers) need the data container for
		/// searching or for caching. Implementors should return all input tables that are searched
		/// for in the Data Container AND all tables that could be transformed tables.
		/// </summary>
		[NotNull]
		IList<IReadOnlyTable> InvolvedTables { get; }

		/// <summary>
		/// The data container containing the cache of the currently processed tile.
		/// </summary>
		[NotNull]
		IDataContainer DataContainer { get; set; }
	}
}

using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.Distributed
{
	/// <summary>
	/// The filter options for a replica dataset.
	/// </summary>
	public interface IDatasetReplicaFilter
	{
		RowsCheckoutType RowsCheckoutType { get; set; }

		string WhereClauseFilter { get; set; }

		/// <summary>
		/// The list of object IDs to be included in the replica.
		/// </summary>
		[CanBeNull]
		List<long> SelectionSet { get; set; }

		bool FilterByGeometry { get; set; }
	}
}

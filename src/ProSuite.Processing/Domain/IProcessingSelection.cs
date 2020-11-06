using System.Collections.Generic;
using ArcGIS.Core.Data;

namespace ProSuite.Processing.Domain
{
	public interface IProcessingSelection
	{
		int SelectionCount { get; }

		int CountSelection(QueryFilter filter = null);

		IEnumerable<Feature> SearchSelection(QueryFilter filter = null, bool recycling = false);
	}
}
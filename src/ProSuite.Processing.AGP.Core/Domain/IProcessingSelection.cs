using System.Collections.Generic;
using ArcGIS.Core.Data;

namespace ProSuite.Processing.AGP.Core.Domain;

public interface IProcessingSelection
{
	int SelectionCount { get; }

	long CountSelection(QueryFilter filter = null);

	IEnumerable<Feature> SearchSelection(QueryFilter filter = null, bool recycling = false);
}

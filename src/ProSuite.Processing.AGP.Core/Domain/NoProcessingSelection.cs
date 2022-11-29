using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;

namespace ProSuite.Processing.AGP.Core.Domain
{
	public class NoProcessingSelection : IProcessingSelection
	{
		public int SelectionCount => 0;

		public long CountSelection(QueryFilter filter = null)
		{
			return 0;
		}

		public IEnumerable<Feature> SearchSelection(QueryFilter filter = null, bool recycling = false)
		{
			return Enumerable.Empty<Feature>();
		}
	}
}

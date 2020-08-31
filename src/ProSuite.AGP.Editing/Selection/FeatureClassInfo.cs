using System.Collections.Generic;
using System.Windows.Documents;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;

namespace ProSuite.AGP.Editing.Selection
{
	public class FeatureClassInfo
	{
		public List<FeatureLayer> BelongingLayers  { get; set; }
		public FeatureClass FeatureClass { get; set; }
		public List<Feature> SelectionCandidates { get; set; }
	}
}

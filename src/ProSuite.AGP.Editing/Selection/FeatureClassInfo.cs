using System.Collections.Generic;
using System.Windows.Documents;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;

namespace ProSuite.AGP.Editing.Selection
{
	public class FeatureClassInfo
	{
		public List<FeatureLayer> BelongingLayers  { get; set; }
		public FeatureClass FeatureClass { get; set; }
		public string FeatureClassName { get; set; }
		public esriGeometryType ShapeType { get; set; }
		public List<Feature> SelectionCandidates { get; set; }
	}
}

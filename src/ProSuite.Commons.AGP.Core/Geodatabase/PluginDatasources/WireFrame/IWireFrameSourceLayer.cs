using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.Geodatabase.PluginDatasources.WireFrame;

public interface IWireFrameSourceLayer
{
	[CanBeNull]
	string FeatureClassName { get; }

	bool Visible { get; }

	RowCursor Search(QueryFilter queryFilter = null);

	[CanBeNull]
	string DefinitionQuery { get; }

	GeometryType GeometryType { get; }

	[CanBeNull]
	Envelope Extent { get; }

	bool Valid { get; }
}

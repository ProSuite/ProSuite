using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;

namespace ProSuite.QA.Container.TestContainer
{
	internal interface ITileEnumContext
	{
		double TileSize { get; }
		TileEnum TileEnum { get; }
		OverlappingFeatures OverlappingFeatures { get; }

		bool IsDisjointFromExecuteArea(IGeometry shape);

		string GetCommonFilterExpression(IReadOnlyTable table);

		UniqueIdProvider GetUniqueIdProvider(IReadOnlyTable table);
	}
}

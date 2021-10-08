using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	public class TrLineToPoly : TrGeometryTransform
	{
		public TrLineToPoly([NotNull] IFeatureClass closedLineClass)
			: base(closedLineClass, esriGeometryType.esriGeometryPolygon) { }

		protected override IGeometry Transform(IGeometry source)
		{
			IPolyline line = (IPolyline)source;
			return GeometryFactory.CreatePolygon(line);
		}
	}

}

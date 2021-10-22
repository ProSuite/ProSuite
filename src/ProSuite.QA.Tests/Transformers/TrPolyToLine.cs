using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	public class TrPolyToLine : TrGeometryTransform
	{
		public TrPolyToLine([NotNull] IFeatureClass featureClass)
			: base(featureClass, esriGeometryType.esriGeometryPolyline) { }

		protected override IEnumerable<IFeature> Transform(IGeometry source)
		{
			IPolygon poly = (IPolygon) source;
			IGeometry transformed = ((ITopologicalOperator) poly).Boundary;

			IFeature feature = CreateFeature();
			feature.Shape = GeometryFactory.Clone(transformed);

			yield return feature;
		}
	}
}

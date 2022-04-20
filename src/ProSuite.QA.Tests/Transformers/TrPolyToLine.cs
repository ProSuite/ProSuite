using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	public class TrPolyToLine : TrGeometryTransform
	{
		[Doc(nameof(DocStrings.TrPolyToLine_0))]
		public TrPolyToLine([NotNull] [Doc(nameof(DocStrings.TrPolyToLine_featureClass))]
		                    IFeatureClass featureClass)
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

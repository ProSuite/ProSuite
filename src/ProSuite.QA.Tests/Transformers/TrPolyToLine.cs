using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[GeometryTransformer]
	public class TrPolyToLine : TrGeometryTransform
	{
		[DocTr(nameof(DocTrStrings.TrPolyToLine_0))]
		public TrPolyToLine(
			[NotNull] [DocTr(nameof(DocTrStrings.TrPolyToLine_featureClass))]
			IReadOnlyFeatureClass featureClass)
			: base(featureClass, esriGeometryType.esriGeometryPolyline) { }

		protected override IEnumerable<GdbFeature> Transform(IGeometry source)
		{
			IPolygon poly = (IPolygon) source;
			IGeometry transformed = ((ITopologicalOperator) poly).Boundary;

			GdbFeature feature = CreateFeature();
			feature.Shape = GeometryFactory.Clone(transformed);

			yield return feature;
		}
	}
}

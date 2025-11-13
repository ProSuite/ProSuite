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
	public class TrPolygonToLine : TrGeometryTransform
	{
		[DocTr(nameof(DocTrStrings.TrPolygonToLine_0))]
		public TrPolygonToLine(
			[NotNull] [DocTr(nameof(DocTrStrings.TrPolygonToLine_featureClass))]
			IReadOnlyFeatureClass featureClass)
			: base(featureClass, esriGeometryType.esriGeometryPolyline) { }

		[InternallyUsedTest]
		public TrPolygonToLine(
			[NotNull] TrPolygonToLineDefinition definition)
			: this((IReadOnlyFeatureClass)definition.FeatureClass) { }

		protected override IEnumerable<GdbFeature> Transform(IGeometry source, long? sourceOid)
		{
			IPolygon poly = (IPolygon) source;
			IGeometry transformed = ((ITopologicalOperator) poly).Boundary;

			GdbFeature feature = CreateFeature(sourceOid);
			feature.Shape = GeometryFactory.Clone(transformed);

			yield return feature;
		}
	}
}

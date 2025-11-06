using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[GeometryTransformer]
	public class TrLineToPolygon : TrGeometryTransform
	{
		private const PolylineConversion _defaultPolylineUsage =
			PolylineConversion.AsPolygonIfClosedElseIgnore;

		[DocTr(nameof(DocTrStrings.TrLineToPolygon_0))]
		public TrLineToPolygon(
			[NotNull] [DocTr(nameof(DocTrStrings.TrLineToPolygon_closedLineClass))]
			IReadOnlyFeatureClass closedLineClass)
			: base(closedLineClass, esriGeometryType.esriGeometryPolygon)
		{
			PolylineUsage = _defaultPolylineUsage;
		}

		[InternallyUsedTest]
		public TrLineToPolygon(
			[NotNull] TrLineToPolygonDefinition definition)
			: this((IReadOnlyFeatureClass) definition.ClosedLineClass)
		{
			PolylineUsage = (PolylineConversion) definition.PolylineUsage;
		}

		[TestParameter(_defaultPolylineUsage)]
		[DocTr(nameof(DocTrStrings.TrLineToPolygon_PolylineUsage))]
		public PolylineConversion PolylineUsage { get; set; }

		protected override IEnumerable<GdbFeature> Transform(IGeometry source, long? sourceOid)
		{
			IPolyline line = (IPolyline) source;
			if (PolylineUsage == PolylineConversion.AsPolygonIfClosedElseIgnore && ! line.IsClosed)
			{
				yield break;
			}

			GdbFeature feature = CreateFeature(sourceOid);
			IPolygon polygon = GeometryFactory.CreatePolygon(line);
			ITopologicalOperator op = (ITopologicalOperator) polygon;
			if (! op.IsSimple)
			{
				op.Simplify();
			}

			feature.Shape = polygon;
			yield return feature;
		}
	}
}

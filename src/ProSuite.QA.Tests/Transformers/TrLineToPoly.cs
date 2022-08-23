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
	public class TrLineToPoly : TrGeometryTransform
	{
		private const PolylineUsage _defaultPolylineUsage =
			PolylineUsage.AsPolygonIfClosedElseIgnore;

		[DocTr(nameof(DocTrStrings.TrLineToPoly_0))]
		public TrLineToPoly(
			[NotNull] [DocTr(nameof(DocTrStrings.TrLineToPoly_closedLineClass))]
			IReadOnlyFeatureClass closedLineClass)
			: base(closedLineClass, esriGeometryType.esriGeometryPolygon)
		{
			PolylineUsage = _defaultPolylineUsage;
		}

		[TestParameter(_defaultPolylineUsage)]
		[DocTr(nameof(DocTrStrings.TrLineToPoly_PolylineUsage))]
		public PolylineUsage PolylineUsage { get; set; }

		protected override IEnumerable<GdbFeature> Transform(IGeometry source)
		{
			IPolyline line = (IPolyline) source;
			if (PolylineUsage == PolylineUsage.AsPolygonIfClosedElseIgnore && ! line.IsClosed)
			{
				yield break;
			}

			GdbFeature feature = CreateFeature();
			feature.Shape = GeometryFactory.CreatePolygon(line);
			yield return feature;
		}
	}
}

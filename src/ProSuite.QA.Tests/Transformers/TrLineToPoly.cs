using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	public class TrLineToPoly : TrGeometryTransform
	{
		private const PolylineUsage _defaultPolylineUsage =
			PolylineUsage.AsPolygonIfClosedElseIgnore;

		[Doc(nameof(DocStrings.TrLineToPoly_0))]
		public TrLineToPoly(
			[NotNull][Doc(nameof(DocStrings.TrLineToPoly_closedLineClass))] IFeatureClass closedLineClass)
			: base(closedLineClass, esriGeometryType.esriGeometryPolygon)
		{
			PolylineUsage = _defaultPolylineUsage;
		}

		[TestParameter(_defaultPolylineUsage)]
		[Doc(nameof(DocStrings.TrLineToPoly_PolylineUsage))]
		public PolylineUsage PolylineUsage { get; set; }

		protected override IEnumerable<IFeature> Transform(IGeometry source)
		{
			IPolyline line = (IPolyline) source;
			if (PolylineUsage == PolylineUsage.AsPolygonIfClosedElseIgnore && ! line.IsClosed)
			{
				yield break;
			}

			IFeature feature = CreateFeature();
			feature.Shape = GeometryFactory.CreatePolygon(line);
			yield return feature;
		}
	}
}
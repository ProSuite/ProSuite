using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry.CreateFootprint;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[GeometryTransformer]
	public class TrFootprint : TrGeometryTransform
	{
		[DocTr(nameof(DocTrStrings.TrFootprint_0))]
		public TrFootprint(
			[NotNull] [DocTr(nameof(DocTrStrings.TrFootprint_multipatchClass))]
			IReadOnlyFeatureClass multipatchClass)
			: base(multipatchClass, esriGeometryType.esriGeometryPolygon) { }

		protected override IEnumerable<GdbFeature> Transform(IGeometry source)
		{
			IMultiPatch patch = (IMultiPatch) source;
			GdbFeature feature = CreateFeature();
			feature.Shape = CreateFootprintUtils.GetFootprint(patch);
			yield return feature;
		}
	}
}

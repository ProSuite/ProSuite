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

		protected override IEnumerable<GdbFeature> Transform(IGeometry source,
		                                                     long? sourceOid)
		{
			IMultiPatch patch = (IMultiPatch) source;

			TransformedFeatureClass transformedClass = GetTransformed();

			GdbFeature feature = sourceOid == null
				                     ? CreateFeature()
				                     : (GdbFeature) transformedClass.CreateObject(sourceOid.Value);

			feature.Shape = CreateFootprintUtils.GetFootprint(patch);
			yield return feature;
		}
	}
}

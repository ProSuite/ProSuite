using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry.CreateFootprint;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	public class TrFootprint : TrGeometryTransform
	{
		[Doc(nameof(DocStrings.TrFootprint_0))]
		public TrFootprint([NotNull] [Doc(nameof(DocStrings.TrFootprint_multipatchClass))]
		                   IFeatureClass multipatchClass)
			: base(multipatchClass, esriGeometryType.esriGeometryPolygon) { }

		protected override IEnumerable<IFeature> Transform(IGeometry source)
		{
			IMultiPatch patch = (IMultiPatch) source;
			IFeature feature = CreateFeature();
			feature.Shape = CreateFootprintUtils.GetFootprint(patch);
			yield return feature;
		}
	}
}
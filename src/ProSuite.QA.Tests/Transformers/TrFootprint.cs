using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry.CreateFootprint;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	public class TrFootprint : TrGeometryTransform
	{
		public TrFootprint([NotNull] IFeatureClass multipatchClass)
			: base(multipatchClass, esriGeometryType.esriGeometryPolygon) { }

		protected override IGeometry Transform(IGeometry source)
		{
			IMultiPatch patch = (IMultiPatch) source;
			return CreateFootprintUtils.GetFootprint(patch);
		}
	}
}

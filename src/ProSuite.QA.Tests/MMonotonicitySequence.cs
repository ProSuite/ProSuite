using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	public class MMonotonicitySequence : MonotonicitySequence
	{
		[CLSCompliant(false)]
		public MMonotonicitySequence(esriMonotinicityEnum monotonicityType,
		                             [CanBeNull] ISpatialReference spatialReference)
			: base(monotonicityType, spatialReference) { }

		[CLSCompliant(false)]
		protected override void AdaptMZAware(IGeometry geometry)
		{
			GeometryUtils.MakeMAware(geometry);
		}
	}
}

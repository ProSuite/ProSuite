using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.LinearNetwork;
using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.Commons.AO.Test.Geometry.LinearNetwork
{
	public class NetworkFeatureFinderMock : LinearNetworkFeatureFinderBase
	{
		public NetworkFeatureFinderMock(params IFeature[] connectedFeatures)
		{
			TargetFeatureCandidates = connectedFeatures.ToList();
		}

		#region Implementation of ILinearNetworkFeatureFinder

		public override ILinearNetworkFeatureFinder Union(ILinearNetworkFeatureFinder other)
		{
			LinearNetworkFeatureFinderBase otherFeatureFinder =
				other as LinearNetworkFeatureFinderBase;

			Assert.NotNull(otherFeatureFinder, "The other feature finder must be of the same type");

			return new LinearNetworkGdbFeatureFinder(UnionTargetFeatureSet(otherFeatureFinder));
		}

		protected override IList<IFeature> ReadFeaturesCore(IGeometry searchGeometry,
		                                                    ICollection<esriGeometryType>
			                                                    geometryTypes)
		{
			throw new UnreachableCodeException("Features should always be read from cache.");
		}

		#endregion

		public bool AreConnected(IFeature feature1, IFeature feature2,
		                         LineEnd feature1LineEndToTest)
		{
			Assert.ArgumentCondition(
				! GdbObjectUtils.IsSameObject(feature1, feature2,
				                              ObjectClassEquality.SameTableSameVersion),
				"The specified features are the same");

			// NOTE: Touches(polyline1, polyline2) is not enough, because Touches() is true when one end
			//		 is snapped to the other line's interior

			var polyline1 = (IPolyline) feature1.Shape;
			var polyline2 = (IPolyline) feature2.Shape;

			bool fromPointTouches = false, toPointTouches = false;

			if (feature1LineEndToTest == LineEnd.From || feature1LineEndToTest == LineEnd.Both)
			{
				fromPointTouches = GeometryUtils.Touches(polyline1.FromPoint, polyline2);
			}

			if (feature1LineEndToTest == LineEnd.To || feature1LineEndToTest == LineEnd.Both)
			{
				toPointTouches = GeometryUtils.Touches(polyline1.ToPoint, polyline2);
			}

			bool result = fromPointTouches || toPointTouches;

			Marshal.ReleaseComObject(polyline1);
			Marshal.ReleaseComObject(polyline2);

			return result;
		}
	}
}

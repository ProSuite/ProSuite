using System.Threading;
using ArcGIS.Core.Geometry;
using NUnit.Framework;

namespace ProSuite.Commons.AGP.Test
{
	/// <summary>
	/// Testing assumptions about ArcGIS.Core from the ArcGIS Pro SDK/API.
	/// </summary>
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class ProCoreTest
	{
		[Test]
		public void CanSpatialReferenceProperties()
		{
			var wgs84 = SpatialReferences.WGS84;
			Assert.IsTrue(wgs84.IsGeographic);
			Assert.IsFalse(wgs84.IsProjected);
			Assert.IsFalse(wgs84.IsUnknown);
		}
	}
}

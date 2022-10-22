using System.Threading;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Hosting;

namespace ProSuite.Commons.AGP.Test
{
	/// <summary>
	/// Testing assumptions about ArcGIS.Core from the ArcGIS Pro SDK/API.
	/// </summary>
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class ProCoreTest
	{
		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			CoreHostProxy.Initialize();
		}

		[Test]
		public void CanSpatialReferenceProperties()
		{
			var wgs84 = SpatialReferences.WGS84;
			Assert.IsTrue(wgs84.IsGeographic);
			Assert.IsFalse(wgs84.IsProjected);
			Assert.IsFalse(wgs84.IsUnknown);
		}

		[Test]
		public void CanMapPointBuilderStatic()
		{
			MapPoint pt1 = MapPointBuilderEx.CreateMapPoint(1.0, 2.0);
			Assert.False(pt1.HasZ);
			Assert.False(pt1.HasM);
			Assert.False(pt1.HasID);
			Assert.False(pt1.IsEmpty);

			MapPoint pt2 = MapPointBuilderEx.CreateMapPoint(1.0, 2.0, 3.0);
			Assert.True(pt2.HasZ);
			Assert.False(pt2.HasM);
			Assert.False(pt2.HasID);
			Assert.False(pt2.IsEmpty);

			MapPoint pt3 = MapPointBuilderEx.CreateMapPoint(1.0, 2.0, 3.0, 4.0);
			Assert.True(pt3.HasZ);
			Assert.True(pt3.HasM);
			Assert.False(pt3.HasID);
			Assert.False(pt3.IsEmpty);

			MapPoint pt3Copy = MapPointBuilderEx.CreateMapPoint(pt3);
			Assert.True(pt3Copy.IsEqual(pt3));
			Assert.False(
				ReferenceEquals(
					pt3, pt3Copy)); // Note: since geoms are immutable, returning *same* would be ok, but here test for behavior
		}

		[Test]
		public void CanMapPointBuilderInstance()
		{
			using (var builder = new MapPointBuilder(1.0, 2.0))
			{
				Assert.False(builder.HasZ);
				Assert.False(builder.HasM);
				Assert.False(builder.HasID);
				Assert.False(builder.IsEmpty);

				var pt = builder.ToGeometry();
				Assert.False(pt.HasZ);
				Assert.False(pt.HasM);
				Assert.False(pt.HasID);
				Assert.False(pt.IsEmpty);
			}

			using (var builder = new MapPointBuilder(1.0, 2.0, 3.0))
			{
				Assert.True(builder.HasZ);
				Assert.False(builder.HasM);
				Assert.False(builder.HasID);
				Assert.False(builder.IsEmpty);

				var pt = builder.ToGeometry();
				Assert.True(pt.HasZ);
				Assert.False(pt.HasM);
				Assert.False(pt.HasID);
				Assert.False(pt.IsEmpty);
			}

			MapPoint pt3;

			using (var builder = new MapPointBuilder(1.0, 2.0, 3.0, 4.0))
			{
				Assert.True(builder.HasZ);
				Assert.True(builder.HasM);
				Assert.False(builder.HasID);
				Assert.False(builder.IsEmpty);

				pt3 = builder.ToGeometry();
				Assert.True(pt3.HasZ);
				Assert.True(pt3.HasM);
				Assert.False(pt3.HasID);
				Assert.False(pt3.IsEmpty);
			}

			using (var builder = new MapPointBuilder(pt3))
			{
				var pt = builder.ToGeometry();
				Assert.True(pt3.IsEqual(pt));
				Assert.False(ReferenceEquals(pt3, pt));
			}
		}
	}
}

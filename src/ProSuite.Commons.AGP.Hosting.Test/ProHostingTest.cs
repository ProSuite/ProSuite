using System.Threading;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Spatial;

namespace ProSuite.Commons.AGP.Hosting.Test
{
	/// <summary>
	/// The tests here confirm that code from ArcGIS Pro SDK can be used
	/// directly and via referenced projects that may use the SDK's NuGet.
	/// <seealso cref="CoreHostProxy" />
	/// </summary>
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class ProHostingTest
	{
		[OneTimeSetUp]
		public void OneTimeSetup()
		{
			CoreHostProxy.Initialize();
		}

		[Test]
		public void CanUseCoreDirectly()
		{
			SpatialReference ch1903 = SpatialReferenceBuilder.CreateSpatialReference(21781);
			MapPoint point = MapPointBuilderEx.CreateMapPoint(600000, 200000, ch1903);

			Assert.AreEqual(600000.0, point.X);
			Assert.AreEqual(200000.0, point.Y);
		}

		[Test]
		public void CanUseCoreViaProject()
		{
			SpatialReference ch1903 = GeometryFactory.CreateSpatialReference(21781);
			MapPoint point = GeometryFactory.CreatePoint(600000, 200000, ch1903);
			int count = GeometryUtils.GetPointCount(point);

			Assert.AreEqual(600000.0, point.X);
			Assert.AreEqual(200000.0, point.Y);
			Assert.AreEqual(1, count);
		}
	}
}

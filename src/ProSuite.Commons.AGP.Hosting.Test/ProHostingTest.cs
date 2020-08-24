using System.Threading;
using NUnit.Framework;
using ProSuite.Commons.AGP.Spatial;
using ArcGIS.Core.Geometry;

namespace ProSuite.Commons.AGP.Hosting.Test
{
	/// <summary>
	/// The tests here confirm that code from ArcGIS Pro SDK can be used
	/// directly and via referenced projects that may use the SDK's NuGet.
	/// <seealso cref="CoreHostProxy"/>
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
			var ch1903 = SpatialReferenceBuilder.CreateSpatialReference(21781);
			var point = MapPointBuilder.CreateMapPoint(600000, 200000, ch1903);

			Assert.AreEqual(600000.0, point.X);
			Assert.AreEqual(200000.0, point.Y);
		}

	    [Test]
	    public void CanUseCoreViaProject()
	    {
		    var ch1903 = GeometryUtils.CreateSpatialReference(21781);
		    var point = GeometryUtils.CreatePoint(600000, 200000, ch1903);

		    Assert.AreEqual(600000.0, point.X);
		    Assert.AreEqual(200000.0, point.Y);
	    }
    }
}

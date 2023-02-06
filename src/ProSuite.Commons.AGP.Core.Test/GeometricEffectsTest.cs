using System.Linq;
using System.Threading;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Hosting;

namespace ProSuite.Commons.AGP.Core.Test;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class GeometricEffectsTest
{
	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		CoreHostProxy.Initialize();
	}

	[Test]
	public void CanDashesUnconstrained1()
	{
		const double delta = 0.00001;

		// line of length 100
		var shape = PolylineBuilderEx.CreatePolyline(
			new[]
			{
				MapPointBuilderEx.CreateMapPoint(0, 0),
				MapPointBuilderEx.CreateMapPoint(100, 0)
			});

		var pattern = new[] { 14.0, 6.0 }; // fits exactly 5 times

		var dashes1 = AssertType<Polyline>(GeometricEffects.Dashes(shape, pattern));
		Assert.AreEqual(5, dashes1.PartCount);

		// same pattern but with positive offset (pat shifted left)
		var dashes2 = AssertType<Polyline>(GeometricEffects.Dashes(shape, pattern, 8.0));
		Assert.AreEqual(6, dashes2.PartCount);
		Assert.AreEqual(6.0, dashes2.Parts.First().Sum(s => s.Length), delta);
		Assert.AreEqual(8.0, dashes2.Parts.Last().Sum(s => s.Length), delta);

		// same pattern but with negative offset (pat shifted right)
		var dashes3 = AssertType<Polyline>(GeometricEffects.Dashes(shape, pattern, -8.0));
		Assert.AreEqual(6, dashes3.PartCount);
		Assert.AreEqual(2.0, dashes3.Parts.First().Sum(s => s.Length), delta);
		Assert.AreEqual(12.0, dashes3.Parts.Last().Sum(s => s.Length), delta);

		// same pattern, positive offset greater than pattern length
		var dashes4 = AssertType<Polyline>(GeometricEffects.Dashes(shape, pattern, 25.0));
		Assert.AreEqual(6, dashes4.PartCount);
		Assert.AreEqual(9.0, dashes4.Parts.First().Sum(s => s.Length), delta);
		Assert.AreEqual(5.0, dashes4.Parts.Last().Sum(s => s.Length), delta);

		// same pattern, negative offset greater than pattern length
		var dashes5 = AssertType<Polyline>(GeometricEffects.Dashes(shape, pattern, -25.0));
		Assert.AreEqual(5, dashes5.PartCount);
		Assert.AreEqual(14.0, dashes5.Parts.First().Sum(s => s.Length), delta);
		Assert.AreEqual(14.0, dashes5.Parts.Last().Sum(s => s.Length), delta);
	}

	[Test]
	public void CanDashesUnconstrained2()
	{
		const double delta = 0.00001;

		// line of length 40
		var shape = PolylineBuilderEx.CreatePolyline(
			new[]
			{
				MapPointBuilderEx.CreateMapPoint(0, 0),
				MapPointBuilderEx.CreateMapPoint(40, 0)
			});

		var pattern = new[] { 15.0, 10.0 };
		var dashes1 = AssertType<Polyline>(GeometricEffects.Dashes(shape, pattern));
		Assert.AreEqual(2, dashes1.PartCount);
		Assert.AreEqual(15.0, dashes1.Parts[0].Sum(s => s.Length), delta);
		Assert.AreEqual(15.0, dashes1.Parts[1].Sum(s => s.Length), delta);

		// Shift pattern left (positive offsetAlongLine)

		var dashes2 = AssertType<Polyline>(GeometricEffects.Dashes(shape, pattern, 10.0));
		Assert.AreEqual(2, dashes2.PartCount);
		Assert.AreEqual(5.0, dashes2.Parts[0].Sum(s => s.Length), delta);
		Assert.AreEqual(15.0, dashes2.Parts[1].Sum(s => s.Length), delta);

		var dashes3 = AssertType<Polyline>(GeometricEffects.Dashes(shape, pattern, 37.0));
		Assert.AreEqual(3, dashes3.PartCount);
		Assert.AreEqual(3.0, dashes3.Parts[0].Sum(s => s.Length), delta);
		Assert.AreEqual(15.0, dashes3.Parts[1].Sum(s => s.Length), delta);
		Assert.AreEqual(2.0, dashes3.Parts[2].Sum(s => s.Length), delta);

		// Shift pattern right (negative offsetAlongLine)

		var dashes4 = AssertType<Polyline>(GeometricEffects.Dashes(shape, pattern, -5.0));
		Assert.AreEqual(2, dashes4.PartCount);
		Assert.AreEqual(15.0, dashes4.Parts[0].Sum(s => s.Length), delta);
		Assert.AreEqual(10.0, dashes4.Parts[1].Sum(s => s.Length), delta);

		var dashes5 = AssertType<Polyline>(GeometricEffects.Dashes(shape, pattern, -27.0));
		Assert.AreEqual(2, dashes5.PartCount);
		Assert.AreEqual(15.0, dashes5.Parts[0].Sum(s => s.Length), delta);
		Assert.AreEqual(13.0, dashes5.Parts[1].Sum(s => s.Length), delta);
	}

	private static T AssertType<T>(object o)
	{
		Assert.IsInstanceOf<T>(o);
		return (T) o;
	}
}

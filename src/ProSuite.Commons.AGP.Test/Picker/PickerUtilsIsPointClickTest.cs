using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.Commons.AGP.Picker;

namespace ProSuite.Commons.AGP.Test.Picker
{
	[TestFixture]
	public class PickerUtilsIsPointClickTest
	{
		private static SpatialReference _sref;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			CoreHostProxy.Initialize();
			_sref = SpatialReferenceBuilder.CreateSpatialReference(2056);
		}

		[Test]
		public void NullGeometry_IsNotPointClick()
		{
			Assert.IsFalse(PickerUtils.IsPointClick(null, 10, out MapPoint clickPoint));
			Assert.IsNull(clickPoint);
		}

		[Test]
		public void EmptyGeometry_IsNotPointClick()
		{
			Polygon empty = new PolygonBuilderEx(_sref).ToGeometry();

			Assert.IsTrue(empty.IsEmpty);
			Assert.IsFalse(PickerUtils.IsPointClick(empty, 10, out MapPoint clickPoint));
			Assert.IsNull(clickPoint);
		}

		[Test]
		public void MapPoint_IsAlwaysPointClick()
		{
			MapPoint point = MapPointBuilderEx.CreateMapPoint(100, 200, _sref);

			// Tolerance is irrelevant for a MapPoint.
			Assert.IsTrue(PickerUtils.IsPointClick(point, 0, out MapPoint clickPoint));
			Assert.IsNotNull(clickPoint);
			Assert.AreEqual(100, clickPoint.X, 1e-9);
			Assert.AreEqual(200, clickPoint.Y, 1e-9);
		}

		[Test]
		public void SmallExtentWithinTolerance_IsPointClick()
		{
			// A tiny 1x1 area with a huge tolerance is treated as a click.
			Polygon small = PolygonBuilderEx.CreatePolygon(
				EnvelopeBuilderEx.CreateEnvelope(0, 0, 1, 1, _sref));

			Assert.IsTrue(PickerUtils.IsPointClick(small, 1e6, out MapPoint clickPoint));
			Assert.IsNotNull(clickPoint);
			// Click point is the extent center.
			Assert.AreEqual(0.5, clickPoint.X, 1e-9);
			Assert.AreEqual(0.5, clickPoint.Y, 1e-9);
		}

		[Test]
		public void LargeExtentBeyondTolerance_IsAreaSelect()
		{
			// A large area with a tiny tolerance is an area selection.
			Polygon large = PolygonBuilderEx.CreatePolygon(
				EnvelopeBuilderEx.CreateEnvelope(0, 0, 1000, 1000, _sref));

			Assert.IsFalse(PickerUtils.IsPointClick(large, 1e-6, out MapPoint clickPoint));
			Assert.IsNull(clickPoint);
		}
	}
}

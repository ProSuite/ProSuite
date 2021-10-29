using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Tests.Test.Construction;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class MeasureUtilsTest
	{
		private const double _mTolerance = 0.01;
		private const double _xyTolerance = 0.01;
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanUseSpacedColumns()
		{
			DataTable tbl = new DataTable();
			tbl.Columns.Add("Year of Change", typeof(string));

			DataView view = new DataView(tbl);
			view.RowFilter = "[Year of Change] = 'a'";
			tbl.Rows.Add("a");
			tbl.Rows.Add("b");

			tbl.AcceptChanges();

			Assert.AreEqual(1, view.Count);
		}

		[Test]
		public void CanGetLinearSubcurve()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, 100))
			                                            .LineTo(CreatePoint(100, 0, 200))
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			IList<IPoint> points;
			IPolyline subcurves = MeasureUtils.GetSubcurves(polyline, 100, 150,
			                                                out points);

			Console.WriteLine(GeometryUtils.ToString(subcurves));

			Assert.IsNotNull(subcurves);
			Assert.AreEqual(50, subcurves.Length);
			Assert.AreEqual(0, points.Count);
		}

		[Test]
		public void CanGetPointSubcurve()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, 100))
			                                            .LineTo(CreatePoint(100, 0, 200))
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			IList<IPoint> points;
			IPolyline subcurves = MeasureUtils.GetSubcurves(polyline, 150, 150,
			                                                out points);

			Console.WriteLine(GeometryUtils.ToString(subcurves));

			Assert.IsNull(subcurves);
			Assert.AreEqual(1, points.Count);
			Assert.AreEqual(50, points[0].X);
			Assert.AreEqual(0, points[0].Y);
		}

		[Test]
		public void CanGetPointSubcurveNonUniqueM()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, 100))
			                                            .LineTo(CreatePoint(50, 0, 200))
			                                            .LineTo(CreatePoint(100, 0, 100))
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			IList<IPoint> points;
			IPolyline subcurves = MeasureUtils.GetSubcurves(polyline, 150, 150,
			                                                out points);

			Console.WriteLine(GeometryUtils.ToString(subcurves));

			Assert.IsNull(subcurves);
			Assert.AreEqual(2, points.Count);
			Assert.AreEqual(25, points[0].X);
			Assert.AreEqual(0, points[0].Y);
			Assert.AreEqual(75, points[1].X);
			Assert.AreEqual(0, points[1].Y);
		}

		[Test]
		public void CanGetSubcurveWithNaNRange()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, 100))
			                                            .LineTo(CreatePoint(100, 0, 200))
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			IList<IPoint> points;
			IPolyline subcurves = MeasureUtils.GetSubcurves(polyline, 150, double.NaN,
			                                                out points);

			Console.WriteLine(GeometryUtils.ToString(subcurves));

			Assert.IsNull(subcurves);
			Assert.AreEqual(1, points.Count);
		}

		[Test]
		public void CanGetSubcurveWithInvertedRange()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, 100))
			                                            .LineTo(CreatePoint(100, 0, 200))
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			IList<IPoint> points;
			IPolyline subcurves = MeasureUtils.GetSubcurves(polyline, 150, 100,
			                                                out points);

			Console.WriteLine(GeometryUtils.ToString(subcurves));

			Assert.IsNotNull(subcurves);
			Assert.AreEqual(1, GeometryUtils.GetPartCount(subcurves));
			Assert.AreEqual(50, subcurves.Length);
			Assert.AreEqual(0, points.Count);
		}

		[Test]
		public void CanGetLinearSubcurveNonUniqueM()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, 100))
			                                            .LineTo(CreatePoint(50, 0, 200))
			                                            .LineTo(CreatePoint(100, 0, 100))
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			IList<IPoint> points;
			IPolyline subcurves = MeasureUtils.GetSubcurves(polyline, 100, 150,
			                                                out points);

			Console.WriteLine(GeometryUtils.ToString(subcurves));

			Assert.IsNotNull(subcurves);
			Assert.AreEqual(50, subcurves.Length);
			Assert.AreEqual(2, ((IGeometryCollection) subcurves).GeometryCount);
			Assert.AreEqual(0, points.Count);
		}

		[Test]
		public void CanGetLinearSubcurveNonUniqueMDisjoint()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, 100))
			                                            .LineTo(CreatePoint(50, 0, 200))
			                                            .LineTo(CreatePoint(100, 0, 100))
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			IList<IPoint> points;
			IPolyline subcurves = MeasureUtils.GetSubcurves(polyline, 150, 200,
			                                                out points);

			Console.WriteLine(GeometryUtils.ToString(subcurves));

			Assert.IsNotNull(subcurves);
			Assert.AreEqual(50, subcurves.Length);
			Assert.AreEqual(2, GeometryUtils.GetPartCount(subcurves));
			Assert.AreEqual(0, points.Count);
		}

		[Test]
		public void CanGetLinearSubcurveConstantSegment()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, 100))
			                                            .LineTo(CreatePoint(50, 0, 200))
			                                            .LineTo(CreatePoint(100, 0, 200))
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			IList<IPoint> points;
			IPolyline subcurves = MeasureUtils.GetSubcurves(polyline, 150, 200,
			                                                out points);

			Console.WriteLine(GeometryUtils.ToString(subcurves));

			Assert.IsNotNull(subcurves);
			Assert.AreEqual(75, subcurves.Length);
			Assert.AreEqual(1, GeometryUtils.GetPartCount(subcurves));
			Assert.AreEqual(0, points.Count);
		}

		[Test]
		public void CanGetLinearSubcurveConstant()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, 200))
			                                            .LineTo(CreatePoint(100, 0, 200))
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			IList<IPoint> points;
			IPolyline subcurves = MeasureUtils.GetSubcurves(polyline, 200, 200,
			                                                out points);

			Console.WriteLine(GeometryUtils.ToString(subcurves));

			Assert.IsNull(subcurves);
			Assert.AreEqual(2, points.Count);
			Assert.AreEqual(0, points[0].X);
			Assert.AreEqual(0, points[0].Y);
			Assert.AreEqual(100, points[1].X);
			Assert.AreEqual(0, points[1].Y);
		}

		[Test]
		public void CanGetLinearSubcurveSmallerThanTolerance()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, 100))
			                                            .LineTo(CreatePoint(100, 0, 200))
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			IList<IPoint> points;
			IPolyline subcurves = MeasureUtils.GetSubcurves(polyline, 150,
			                                                150 + _mTolerance / 100,
			                                                out points);

			Console.WriteLine(GeometryUtils.ToString(subcurves));

			Assert.IsNull(subcurves);
			Assert.AreEqual(1, points.Count);
			Assert.AreEqual(50, points[0].X);
			Assert.AreEqual(0, points[0].Y);
		}

		[Test]
		public void CanGetLinearSubcurveLargerThanTolerance()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, 100))
			                                            .LineTo(CreatePoint(100, 0, 200))
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			IList<IPoint> points;
			IPolyline subcurves = MeasureUtils.GetSubcurves(polyline, 150,
			                                                150 + _mTolerance * 2, out points);

			Console.WriteLine(GeometryUtils.ToString(subcurves));

			Assert.IsNotNull(subcurves);
			Assert.AreEqual(0.020000000000010232, subcurves.Length);
			Assert.AreEqual(0, points.Count);
		}

		[Test]
		public void CanGetOvershootingLinearSubcurveSmallerThanTolerance()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, 100))
			                                            .LineTo(CreatePoint(100, 0, 200))
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			IList<IPoint> points;
			IPolyline subcurves = MeasureUtils.GetSubcurves(polyline, 200, 300,
			                                                out points);

			Console.WriteLine(GeometryUtils.ToString(subcurves));

			Assert.IsNull(subcurves);
			Assert.AreEqual(1, points.Count);
			Assert.AreEqual(100, points[0].X);
			Assert.AreEqual(0, points[0].Y);
		}

		[Test]
		public void CanGetAllMonotonicityTypes()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, 100))
			                                            .LineTo(CreatePoint(50, 0, 200))
			                                            .LineTo(CreatePoint(100, 0, 100))
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			Assert.IsTrue(MeasureUtils.ContainsAllMonotonicityTypes(
				              polyline,
				              esriMonotinicityEnum.esriValueIncreases));

			Assert.IsTrue(MeasureUtils.ContainsAllMonotonicityTypes(
				              polyline,
				              esriMonotinicityEnum.esriValueIncreases,
				              esriMonotinicityEnum.esriValueDecreases));

			Assert.IsFalse(MeasureUtils.ContainsAllMonotonicityTypes(
				               polyline,
				               esriMonotinicityEnum.esriValueIncreases,
				               esriMonotinicityEnum.esriValueLevel));
		}

		#region Monotonicity

		[Test]
		public void CanGetMonotonicityTypeDecreasing()
		{
			ISegment segment = new LineClass
			                   {
				                   FromPoint = CreatePoint(0, 0, 10),
				                   ToPoint = CreatePoint(0, 1, 0)
			                   };

			Assert.IsTrue(MeasureUtils.GetMonotonicityType(segment) ==
			              esriMonotinicityEnum.esriValueDecreases);
		}

		[Test]
		public void CanGetMonotonicityTypeIncreasing()
		{
			ISegment segment = new LineClass
			                   {
				                   FromPoint = CreatePoint(0, 0, 0),
				                   ToPoint = CreatePoint(0, 1, 0.00000001)
			                   };

			Assert.IsTrue(MeasureUtils.GetMonotonicityType(segment) ==
			              esriMonotinicityEnum.esriValueIncreases);
		}

		[Test]
		public void CanGetMonotonicityTypeLevel()
		{
			ISegment segment = new LineClass
			                   {
				                   FromPoint = CreatePoint(0, 0, 0),
				                   ToPoint = CreatePoint(0, 1, 0)
			                   };

			Assert.IsTrue(MeasureUtils.GetMonotonicityType(segment) ==
			              esriMonotinicityEnum.esriValueLevel);
		}

		[Test]
		public void CanGetMonotonicityTypeEmpty()
		{
			ISegment segmentNaNFirst = new LineClass
			                           {
				                           FromPoint = CreatePoint(0, 0, double.NaN),
				                           ToPoint = CreatePoint(0, 1, 0)
			                           };

			ISegment segmentNaNSecond = new LineClass
			                            {
				                            FromPoint = CreatePoint(0, 0, 99999.88),
				                            ToPoint = CreatePoint(0, 1, double.NaN)
			                            };

			ISegment segmentNaNBoth = new LineClass
			                          {
				                          FromPoint = CreatePoint(0, 0, double.NaN),
				                          ToPoint = CreatePoint(0, 1, double.NaN)
			                          };

			Assert.IsTrue(MeasureUtils.GetMonotonicityType(segmentNaNFirst) ==
			              esriMonotinicityEnum.esriValuesEmpty);

			Assert.IsTrue(MeasureUtils.GetMonotonicityType(segmentNaNSecond) ==
			              esriMonotinicityEnum.esriValuesEmpty);

			Assert.IsTrue(MeasureUtils.GetMonotonicityType(segmentNaNBoth) ==
			              esriMonotinicityEnum.esriValuesEmpty);
		}

		[Test]
		public void CanGetMonotonicitySequencesDecreasing1()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, 100))
			                                            .LineTo(CreatePoint(50, 0, 200))
			                                            .LineTo(CreatePoint(100, 0, 100))
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			IEnumerable<MMonotonicitySequence> result =
				MeasureUtils.GetMonotonicitySequences((ISegmentCollection) polyline,
				                                      esriMonotinicityEnum.esriValueDecreases);
			Assert.AreEqual(1, result.Count());

			MMonotonicitySequence[] sequences = result.ToArray();

			Assert.IsTrue(sequences[0].MonotonicityType ==
			              esriMonotinicityEnum.esriValueDecreases);

			Assert.AreEqual(1, sequences[0].Segments.Count);

			Assert.AreEqual(sequences[0].Segments[0].FromPoint.M, 200);
			Assert.AreEqual(sequences[0].Segments[0].ToPoint.M, 100);
		}

		[Test]
		public void CanGetMonotonicitySequencesDecreasing2()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, -100))
			                                            .LineTo(CreatePoint(1, 0, -200))
			                                            .LineTo(CreatePoint(2, 0, -300))
			                                            .LineTo(CreatePoint(3, 0, -400))
			                                            .LineTo(CreatePoint(4, 0, -300))
			                                            .LineTo(CreatePoint(5, 0, -200))
			                                            .LineTo(CreatePoint(6, 0, -300))
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			IEnumerable<MMonotonicitySequence> result =
				MeasureUtils.GetMonotonicitySequences((ISegmentCollection) polyline,
				                                      esriMonotinicityEnum.esriValueDecreases);
			Assert.IsTrue(2 == result.Count());

			MMonotonicitySequence[] sequences = result.ToArray();

			Assert.IsTrue(sequences[0].MonotonicityType ==
			              esriMonotinicityEnum.esriValueDecreases);

			Assert.AreEqual(3, sequences[0].Segments.Count);

			Assert.AreEqual(-100, sequences[0].Segments[0].FromPoint.M);
			Assert.AreEqual(-200, sequences[0].Segments[0].ToPoint.M);
			Assert.AreEqual(-200, sequences[0].Segments[1].FromPoint.M);
			Assert.AreEqual(-300, sequences[0].Segments[1].ToPoint.M);
			Assert.AreEqual(-300, sequences[0].Segments[2].FromPoint.M);
			Assert.AreEqual(-400, sequences[0].Segments[2].ToPoint.M);

			Assert.IsTrue(sequences[1].MonotonicityType ==
			              esriMonotinicityEnum.esriValueDecreases);

			Assert.AreEqual(1, sequences[1].Segments.Count);

			Assert.AreEqual(-200, sequences[1].Segments[0].FromPoint.M);
			Assert.AreEqual(-300, sequences[1].Segments[0].ToPoint.M);
		}

		[Test]
		public void CanGetMonotonicitySequencesDecreasingAndIncreasing()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, 0))
			                                            .LineTo(CreatePoint(1, 0, 1))
			                                            .LineTo(CreatePoint(2, 0, 8))
			                                            .LineTo(CreatePoint(3, 0, 2))
			                                            .LineTo(CreatePoint(4, 0, 12))
			                                            .LineTo(CreatePoint(5, 0, 0))
			                                            .LineTo(CreatePoint(6, 0, -1))
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			var monotonicityTypes = new[]
			                        {
				                        esriMonotinicityEnum.esriValueDecreases,
				                        esriMonotinicityEnum.esriValueIncreases
			                        };

			IEnumerable<MMonotonicitySequence> result =
				MeasureUtils.GetMonotonicitySequences((ISegmentCollection) polyline,
				                                      monotonicityTypes);
			Assert.AreEqual(4, result.Count());

			MMonotonicitySequence[] sequences = result.ToArray();

			Assert.IsTrue(sequences[0].MonotonicityType ==
			              esriMonotinicityEnum.esriValueIncreases);

			Assert.AreEqual(2, sequences[0].Segments.Count);

			Assert.AreEqual(0, sequences[0].Segments[0].FromPoint.M);
			Assert.AreEqual(1, sequences[0].Segments[0].ToPoint.M);
			Assert.AreEqual(1, sequences[0].Segments[1].FromPoint.M);
			Assert.AreEqual(8, sequences[0].Segments[1].ToPoint.M);

			Assert.IsTrue(sequences[1].MonotonicityType ==
			              esriMonotinicityEnum.esriValueDecreases);

			Assert.AreEqual(1, sequences[1].Segments.Count);

			Assert.AreEqual(8, sequences[1].Segments[0].FromPoint.M);
			Assert.AreEqual(2, sequences[1].Segments[0].ToPoint.M);

			Assert.IsTrue(sequences[2].MonotonicityType ==
			              esriMonotinicityEnum.esriValueIncreases);

			Assert.AreEqual(1, sequences[2].Segments.Count);

			Assert.AreEqual(2, sequences[2].Segments[0].FromPoint.M);
			Assert.AreEqual(12, sequences[2].Segments[0].ToPoint.M);

			Assert.IsTrue(sequences[3].MonotonicityType ==
			              esriMonotinicityEnum.esriValueDecreases);

			Assert.AreEqual(2, sequences[3].Segments.Count);

			Assert.AreEqual(12, sequences[3].Segments[0].FromPoint.M);
			Assert.AreEqual(0, sequences[3].Segments[0].ToPoint.M);
			Assert.AreEqual(0, sequences[3].Segments[1].FromPoint.M);
			Assert.AreEqual(-1, sequences[3].Segments[1].ToPoint.M);
		}

		[Test]
		public void CanGetMonotonicitySequencesEmpty()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, 100))
			                                            .LineTo(CreatePoint(50, 0, double.NaN))
			                                            .LineTo(CreatePoint(100, 0, 100))
			                                            .LineTo(CreatePoint(50, 0, double.NaN))
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			IEnumerable<MMonotonicitySequence> result =
				MeasureUtils.GetMonotonicitySequences((ISegmentCollection) polyline,
				                                      esriMonotinicityEnum.esriValuesEmpty);
			Assert.AreEqual(1, result.Count());

			MMonotonicitySequence[] sequences = result.ToArray();

			Assert.IsTrue(sequences[0].MonotonicityType ==
			              esriMonotinicityEnum.esriValuesEmpty);

			Assert.AreEqual(3, sequences[0].Segments.Count);

			Assert.AreEqual(100, sequences[0].Segments[0].FromPoint.M);
			Assert.IsTrue(double.IsNaN(sequences[0].Segments[0].ToPoint.M));
			Assert.IsTrue(double.IsNaN(sequences[0].Segments[1].FromPoint.M));
			Assert.AreEqual(100, sequences[0].Segments[1].ToPoint.M);
			Assert.AreEqual(100, sequences[0].Segments[2].FromPoint.M);
			Assert.IsTrue(double.IsNaN(sequences[0].Segments[2].ToPoint.M));
		}

		[Test]
		public void CanGetMonotonicityTrendEmpty()
		{
			var polyline =
				(IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, double.NaN))
				                             .LineTo(CreatePoint(50, 0, double.NaN))
				                             .LineTo(CreatePoint(150, 0, double.NaN))
				                             .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			esriMonotinicityEnum result =
				MeasureUtils.GetMonotonicityTrend(polyline);

			Assert.AreEqual(esriMonotinicityEnum.esriValuesEmpty, result);
		}

		[Test]
		public void CanGetMonotonicityTrendIncreasing1()
		{
			var polyline =
				(IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, -0.000001))
				                             .LineTo(CreatePoint(50, 1, double.NaN))
				                             .LineTo(CreatePoint(51, 2, double.NaN))
				                             .LineTo(CreatePoint(52, 3, double.NaN))
				                             .LineTo(CreatePoint(53, 4, -0.0000001))
				                             .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			esriMonotinicityEnum result =
				MeasureUtils.GetMonotonicityTrend(polyline);

			Assert.AreEqual(esriMonotinicityEnum.esriValueIncreases, result);
		}

		[Test]
		public void CanGetMonotonicityTrendIncreasing2()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, 0))
			                                            .LineTo(CreatePoint(50, 1, double.NaN))
			                                            .LineTo(CreatePoint(51, 2, double.NaN))
			                                            .LineTo(CreatePoint(52, 3, 0.1))
			                                            .LineTo(CreatePoint(53, 4, double.NaN))
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			esriMonotinicityEnum result =
				MeasureUtils.GetMonotonicityTrend(polyline);

			Assert.AreEqual(esriMonotinicityEnum.esriValueIncreases, result);
		}

		[Test]
		public void CanGetMonotonicityTrendIncreasing3()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, 0))
			                                            .LineTo(CreatePoint(1, 0.0000001, 1))
			                                            .LineTo(CreatePoint(2, 0, 2))
			                                            .LineTo(CreatePoint(3, 0, 1))
			                                            .LineTo(CreatePoint(4, 0, 0))
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			esriMonotinicityEnum result =
				MeasureUtils.GetMonotonicityTrend(polyline);

			Assert.AreEqual(esriMonotinicityEnum.esriValueIncreases, result);
		}

		[Test]
		public void CanGetMonotonicityTrendLevelSymmetric()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, 0))
			                                            .LineTo(CreatePoint(1, 0, 1))
			                                            .LineTo(CreatePoint(2, 0, 2))
			                                            .LineTo(CreatePoint(3, 0, 1))
			                                            .LineTo(CreatePoint(4, 0, 0))
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			esriMonotinicityEnum result =
				MeasureUtils.GetMonotonicityTrend(polyline);

			Assert.AreEqual(esriMonotinicityEnum.esriValueLevel, result);
		}

		[Test]
		public void CanGetMonotonicityTrendLevelCompletelyLevel()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, 0))
			                                            .LineTo(CreatePoint(1, 0, 0))
			                                            .LineTo(CreatePoint(2, 0, 0))
			                                            .LineTo(CreatePoint(3, 0, double.NaN))
			                                            .LineTo(CreatePoint(4, 0, 0))
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			esriMonotinicityEnum result =
				MeasureUtils.GetMonotonicityTrend(polyline);

			Assert.AreEqual(esriMonotinicityEnum.esriValueLevel, result);
		}

		[Test]
		public void CanGetMonotonicityTrendLevelOnlyOneValueLast()
		{
			var polyline =
				(IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, double.NaN))
				                             .LineTo(CreatePoint(1, 0, double.NaN))
				                             .LineTo(CreatePoint(2, 0, double.NaN))
				                             .LineTo(CreatePoint(3, 0, double.NaN))
				                             .LineTo(CreatePoint(4, 0, 0))
				                             .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			esriMonotinicityEnum result =
				MeasureUtils.GetMonotonicityTrend(polyline);

			Assert.AreEqual(esriMonotinicityEnum.esriValueLevel, result);
		}

		[Test]
		public void CanGetMonotonicityTrendLevelOnlyOneValueFirst()
		{
			var polyline =
				(IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, 999989999))
				                             .LineTo(CreatePoint(1, 0, double.NaN))
				                             .LineTo(CreatePoint(2, 0, double.NaN))
				                             .LineTo(CreatePoint(3, 0, double.NaN))
				                             .LineTo(CreatePoint(4, 0, double.NaN))
				                             .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			esriMonotinicityEnum result =
				MeasureUtils.GetMonotonicityTrend(polyline);

			Assert.AreEqual(esriMonotinicityEnum.esriValueLevel, result);
		}

		[Test]
		public void CanGetMonotonicityTrendLevelOnlyOneValueInBetween()
		{
			var polyline =
				(IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, double.NaN))
				                             .LineTo(CreatePoint(1, 0, 0))
				                             .LineTo(CreatePoint(2, 0, double.NaN))
				                             .LineTo(CreatePoint(4, 0, double.NaN))
				                             .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			esriMonotinicityEnum result =
				MeasureUtils.GetMonotonicityTrend(polyline);

			Assert.AreEqual(esriMonotinicityEnum.esriValueLevel, result);
		}

		[Test]
		public void CanGetMonotonicityTrendDecreasing1()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, -0.00001))
			                                            .LineTo(CreatePoint(4, 0, -0.1))
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			esriMonotinicityEnum result =
				MeasureUtils.GetMonotonicityTrend(polyline);

			Assert.AreEqual(esriMonotinicityEnum.esriValueDecreases, result);
		}

		[Test]
		public void CanGetMonotonicityTrendDecreasing2()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, 0))
			                                            .LineTo(CreatePoint(0.5, 0, 1))
			                                            .LineTo(CreatePoint(1, 0, 1.1))
			                                            .LineTo(CreatePoint(1.5, 0.001, 1.10001))
			                                            .LineTo(CreatePoint(2, 0, 2))
			                                            .LineTo(CreatePoint(3, 0.002, 1))
			                                            .LineTo(CreatePoint(4, 0, 0))
			                                            .LineTo(CreatePoint(5, 0, 999999))
			                                            .LineTo(CreatePoint(6, 0, 0))
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			esriMonotinicityEnum result =
				MeasureUtils.GetMonotonicityTrend(polyline);

			Assert.AreEqual(esriMonotinicityEnum.esriValueDecreases, result);
		}

		[Test]
		public void CanGetErrorSequences()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, 0))
			                                            .LineTo(CreatePoint(1, 0, 1)) // +
			                                            .LineTo(CreatePoint(1, 0, 2)) // +
			                                            .LineTo(CreatePoint(2, 0, 3)) // +
			                                            .LineTo(CreatePoint(3, 0, 3)) // =
			                                            .LineTo(CreatePoint(4, 0, 3)) // =
			                                            .LineTo(CreatePoint(5, 0, 1)) // -
			                                            .LineTo(CreatePoint(6, 0, 0)) // - 
			                                            .LineTo(CreatePoint(7, 0, 9)) // +
			                                            .LineTo(CreatePoint(8, 0, 4)) // -
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			IEnumerable<MMonotonicitySequence> result;
			MMonotonicitySequence[] sequences;

			result = MeasureUtils.GetErrorSequences(polyline, MonotonicityDirection.Increasing,
			                                        () => false, true);
			sequences = result.ToArray();

			Assert.AreEqual(result.Count(), 2);
			Assert.AreEqual(sequences[0].Segments.Count, 2);
			Assert.AreEqual(sequences[1].Segments.Count, 1);

			result = MeasureUtils.GetErrorSequences(polyline, MonotonicityDirection.Increasing,
			                                        () => false, false);
			sequences = result.ToArray();

			Assert.AreEqual(result.Count(), 3);
			Assert.AreEqual(sequences[0].Segments.Count, 2);
			Assert.AreEqual(sequences[1].Segments.Count, 2);
			Assert.AreEqual(sequences[2].Segments.Count, 1);

			result = MeasureUtils.GetErrorSequences(polyline, MonotonicityDirection.Increasing,
			                                        () => true, true);
			sequences = result.ToArray();

			Assert.AreEqual(result.Count(), 2);
			Assert.AreEqual(sequences[0].Segments.Count, 3);
			Assert.AreEqual(sequences[1].Segments.Count, 1);

			result = MeasureUtils.GetErrorSequences(polyline, MonotonicityDirection.Any,
			                                        () => false, true);
			sequences = result.ToArray();

			Assert.AreEqual(result.Count(), 2);
			Assert.AreEqual(sequences[0].Segments.Count, 2);
			Assert.AreEqual(sequences[1].Segments.Count, 1);

			result = MeasureUtils.GetErrorSequences(polyline, MonotonicityDirection.Any,
			                                        () => false, false);
			sequences = result.ToArray();

			Assert.AreEqual(result.Count(), 3);
			Assert.AreEqual(sequences[0].Segments.Count, 2);
			Assert.AreEqual(sequences[1].Segments.Count, 2);
			Assert.AreEqual(sequences[2].Segments.Count, 1);
		}

		[Test]
		public void CanGetErrorSequences2()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, 0))
			                                            .LineTo(CreatePoint(1, 0, 1)) // +
			                                            .LineTo(CreatePoint(1, 0, 2)) // +
			                                            .LineTo(CreatePoint(2, 0, 2)) // =
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			IEnumerable<MMonotonicitySequence> result;
			MMonotonicitySequence[] sequences;

			result = MeasureUtils.GetErrorSequences(polyline, MonotonicityDirection.Increasing,
			                                        () => false, true);

			Assert.AreEqual(result.Count(), 0);

			result = MeasureUtils.GetErrorSequences(polyline, MonotonicityDirection.Increasing,
			                                        () => false, false);
			sequences = result.ToArray();

			Assert.AreEqual(result.Count(), 1);
			Assert.AreEqual(sequences[0].Segments.Count, 1);
		}

		[Test]
		public void CanGetErrorSequences3()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(CreatePoint(0, 0, 0))
			                                            .LineTo(CreatePoint(1, 0, 1)) // +
			                                            .LineTo(CreatePoint(1, 0, double.NaN))
			                                            .LineTo(CreatePoint(2, 0, 2)) // =
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference(_mTolerance, _xyTolerance);
			GeometryUtils.MakeMAware(polyline);

			IEnumerable<MMonotonicitySequence> result;
			MMonotonicitySequence[] sequences;

			result = MeasureUtils.GetErrorSequences(polyline, MonotonicityDirection.Increasing,
			                                        () => false, true);

			Assert.AreEqual(0, result.Count());

			result = MeasureUtils.GetErrorSequences(polyline, MonotonicityDirection.Decreasing,
			                                        () => false, true);
			sequences = result.ToArray();

			Assert.AreEqual(1, result.Count());
			Assert.AreEqual(1, sequences[0].Segments.Count);
		}

		#endregion

		[NotNull]
		private static ISpatialReference CreateSpatialReference(double mTolerance,
		                                                        double xyTolerance)
		{
			ISpatialReference result = SpatialReferenceUtils.CreateSpatialReference
				((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);
			SpatialReferenceUtils.SetMDomain(result, 0, 1000000, mTolerance / 10, mTolerance);
			SpatialReferenceUtils.SetXYDomain(result, -1000, -1000, 100000, 100000,
			                                  xyTolerance / 10, xyTolerance);
			return result;
		}

		[NotNull]
		private static IPoint CreatePoint(double x, double y, double m)
		{
			IPoint result = GeometryFactory.CreatePoint(x, y);

			((IMAware) result).MAware = true;
			result.M = m;

			return result;
		}
	}
}

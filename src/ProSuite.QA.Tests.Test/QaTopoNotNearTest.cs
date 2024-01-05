using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Tests.Coincidence;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaTopoNotNearTest
	{
		private IFeatureWorkspace _testWs;
		private ISpatialReference _spatialReference;
		private const string _stateIdFieldName = "STATE";
		private const string _textFieldName = "FLD_TEXT";
		private const string _doubleFieldName = "FLD_DOUBLE";
		private const string _dateFieldName = "FLD_DATE";
		private const double _xyTolerance = 0.001;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();

			_spatialReference = CreateLV95();
			_testWs = TestWorkspaceUtils.CreateInMemoryWorkspace(
				"QaTopoNotNearTest");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void TestNotNear()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1, CurveConstruction.StartLine(0, 0).LineTo(10, 10).Curve);
			AddFeature(fc1, CurveConstruction.StartLine(0, 5).LineTo(10, 5).Curve);

			var test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 1, 2);

			AssertErrors(2, Run(test, 1000));
		}

		[Test]
		public void TestCrossing()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1, CurveConstruction.StartLine(0, 0).LineTo(10, 10).Curve);
			AddFeature(fc1, CurveConstruction.StartLine(0, 5).LineTo(4.5, 5).Curve);
			AddFeature(fc1, CurveConstruction.StartLine(4.5, 5).LineTo(10, 5).Curve);

			var test = new QaTopoNotNear(ReadOnlyTableFactory.Create(fc1), 1, 2.1);
			test.CrossingMinLengthFactor = 20;

			AssertErrors(0, Run(test, 1000));
		}

		[Test]
		public void TestCrossingMultiple()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1, CurveConstruction.StartLine(0, 5)
			                                 .LineTo(10, 5)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(3.9, 0)
			                                 .LineTo(3.9, 5.1)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(3.9, 5.1)
			                                 .LineTo(3.9, 10)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(5.1, 0)
			                                 .LineTo(5.1, 10)
			                                 .Curve,
			           doubleValue: 1);

			// var test = new QaTopoNotNear(fc1, 1, 0.3);
			var test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 1, _doubleFieldName, 5, 0, false);

			//var test = new QaTopoNotNear(fc1, 1, "IIF(ObjectId=1, 1, IIF(ObjectId=2, 1, IIF(ObjectId=3, 1, 1)))", 5, 0, false);

			test.CrossingMinLengthFactor = 20;

			Run(test, 1000);
		}

		[Test]
		public void TestCrossingMultiple1()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1, CurveConstruction.StartLine(2, 0)
			                                 .LineTo(2, 5)
			                                 .LineTo(2, 10)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(1, 9)
			                                 .LineTo(10, 9)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(1, 5)
			                                 .LineTo(2, 5)
			                                 .LineTo(3.5, 5)
			                                 .Curve,
			           doubleValue: 2);
			AddFeature(fc1, CurveConstruction.StartLine(3.5, 5)
			                                 .LineTo(10, 5)
			                                 .Curve,
			           doubleValue: 2);

			// var test = new QaTopoNotNear(fc1, 1, 0.3);
			var test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 1, _doubleFieldName, 5, 0, false);
			test.CrossingMinLengthFactor = 20;

			Run(test, 1000);
		}

		[Test]
		[Category(Commons.Test.TestCategory.NoContainer)]
		public void TestNotReported()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			//AddFeature(fc1, CurveConstruction.StartLine(2, 0).LineTo(2, 10).Curve,
			//		   doubleValue: 1, textFieldValue: "A");
			//AddFeature(fc1, CurveConstruction.StartLine(0, 5).LineTo(4.5, 5).Curve,
			//		   doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(4.5, 5)
			                                 .LineTo(12, 5)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(8, 0)
			                                 .LineTo(8, 6)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1,
			           CurveConstruction.StartLine(8, 10)
			                            .LineTo(8, 9)
			                            .LineTo(8, 6.5)
			                            .LineTo(8, 6)
			                            .Curve,
			           doubleValue: 1, textFieldValue: "A");

			var test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 1, _doubleFieldName, 5, 1.6, false);
			test.NotReportedCondition =
				$"G1.{_textFieldName} = 'A' OR G2.{_textFieldName} = 'A'";

			Run(test, 1000);
		}

		[Test]
		public void TestSmallConnected()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1, CurveConstruction.StartLine(0, 0)
			                                 .LineTo(10, 10)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(0, 0)
			                                 .LineTo(1, 1.1)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(10, 10)
			                                 .LineTo(10, 8.5)
			                                 .Curve,
			           doubleValue: 1);

			var test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 1, _doubleFieldName, 5, 1.6, false);

			Assert.AreEqual(1, Run(test, 1000).Count);
		}

		[Test]
		public void TestSmallDisjoint()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1, CurveConstruction.StartLine(0, 0)
			                                 .LineTo(10, 10)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(5, 6)
			                                 .LineTo(6, 5)
			                                 .Curve,
			           doubleValue: 1);

			AddFeature(fc1, CurveConstruction.StartLine(0, 1.5)
			                                 .LineTo(1.5, 0)
			                                 .Curve,
			           doubleValue: 1);

			var test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 1, _doubleFieldName, 5, 5, false);

			Assert.AreEqual(1, Run(test, 1000).Count);
		}

		[Test]
		public void TestNotReportedCoincident()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1,
			           CurveConstruction.StartLine(0, 5)
			                            .LineTo(5, 6)
			                            .LineTo(7, 6)
			                            .LineTo(10, 5)
			                            .Curve,
			           doubleValue: 1, textFieldValue: "A");
			AddFeature(fc1, CurveConstruction.StartLine(5, 6)
			                                 .LineTo(5, 0)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(10, 5)
			                                 .LineTo(7, 6)
			                                 .LineTo(5, 6)
			                                 .Curve,
			           doubleValue: 1);
			//AddFeature(fc1, CurveConstruction.StartLine(10, 5).LineTo(10, 8).Curve,
			//		   doubleValue: 3);

			var test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 1, _doubleFieldName, 1, 0, false);
			test.NotReportedCondition =
				$"G1.{_textFieldName} = 'A' OR G2.{_textFieldName} = 'A'";

			Run(test, 1000);
		}

		[Test]
		public void TestNotReportedNearCoincident()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1, CurveConstruction.StartLine(1, 5)
			                                 .LineTo(10, 5)
			                                 .Curve,
			           doubleValue: 0, textFieldValue: "A");
			AddFeature(fc1, CurveConstruction.StartLine(0, 5)
			                                 .LineTo(1, 5)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1,
			           CurveConstruction.StartLine(0, 5.5)
			                            .LineTo(1.5, 5.5)
			                            .LineTo(10, 5.5)
			                            .Curve,
			           doubleValue: 1);

			var test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 1, _doubleFieldName, 4, 3, false);
			test.CrossingMinLengthFactor = 3.5;
			test.NotReportedCondition =
				$"G1.{_textFieldName} = 'A' OR G2.{_textFieldName} = 'A'";

			Run(test, 1000);
		}

		[Test]
		public void TestNotReportedLineCapSimple()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1, CurveConstruction.StartLine(5, 5)
			                                 .LineTo(5, 10)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1,
			           CurveConstruction.StartLine(0, 3.8)
			                            .LineTo(10, 3.8)
			                            .Curve,
			           doubleValue: 1);
			AddFeature(fc1,
			           CurveConstruction.StartLine(0, 3.8)
			                            .LineTo(-2, 3.8)
			                            .Curve,
			           doubleValue: 1);
			AddFeature(fc1,
			           CurveConstruction.StartLine(10, 3.8)
			                            .LineTo(12, 3.8)
			                            .Curve,
			           doubleValue: 1);

			var test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 1, _doubleFieldName, 4, 1, false);
			Assert.AreEqual(1, Run(test, 1000).Count);

			test.UnconnectedLineCapStyle = LineCapStyle.Butt;
			Assert.AreEqual(0, Run(test, 1000).Count);
		}

		[Test]
		[Category(Commons.Test.TestCategory.FixMe)]
		public void TestNotReportedLineCapWithAura()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1, CurveConstruction.StartLine(5, 5)
			                                 .LineTo(5, 10)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1,
			           CurveConstruction.StartLine(0, 3.8)
			                            .LineTo(10, 3.8)
			                            .Curve,
			           doubleValue: 1);
			AddFeature(fc1,
			           CurveConstruction.StartLine(0, 3.8)
			                            .LineTo(-2, 3.8)
			                            .Curve,
			           doubleValue: 1);
			AddFeature(fc1,
			           CurveConstruction.StartLine(10, 3.8)
			                            .LineTo(12, 3.8)
			                            .Curve,
			           doubleValue: 1);

			AddFeature(fc1, CurveConstruction.StartLine(5, 5)
			                                 .LineTo(10, 10)
			                                 .Curve,
			           doubleValue: 0, textFieldValue: "A");

			var test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 1, _doubleFieldName, 4, 1, false);
			test.NotReportedCondition =
				$"G1.{_textFieldName} = 'A' OR G2.{_textFieldName} = 'A'";

			Assert.AreEqual(1, Run(test, 1000).Count);

			test.UnconnectedLineCapStyle = LineCapStyle.Butt;
			Assert.AreEqual(0, Run(test, 1000).Count);
		}

		[Test]
		[Category(Commons.Test.TestCategory.FixMe)]
		public void TestNotReportedNearCoincidentLineCap()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1, CurveConstruction.StartLine(1, 5)
			                                 .LineTo(10, 5)
			                                 .Curve,
			           doubleValue: 0, textFieldValue: "A");
			AddFeature(fc1, CurveConstruction.StartLine(1, 5)
			                                 .LineTo(1, 2)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1,
			           CurveConstruction.StartLine(10, 5.5)
			                            .LineTo(1.5, 5.5)
			                            .LineTo(1, 5.5)
			                            .Curve,
			           doubleValue: 1);

			var test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 1, _doubleFieldName, 4, 1, false);
			test.UnconnectedLineCapStyle = LineCapStyle.Butt;
			test.NotReportedCondition =
				$"G1.{_textFieldName} = 'A' OR G2.{_textFieldName} = 'A'";

			Run(test, 1000);
		}

		[Test]
		public void TestNotReportedNearCoincident2()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1, CurveConstruction.StartLine(10, 9.9)
			                                 .LineTo(10, 12.1)
			                                 .Curve,
			           doubleValue: 0, textFieldValue: "A");
			AddFeature(fc1, CurveConstruction.StartLine(10, 12.1)
			                                 .LineTo(10, 15)
			                                 .Curve,
			           doubleValue: 1);

			AddFeature(fc1, CurveConstruction.StartLine(0, 11)
			                                 .LineTo(10.5, 11)
			                                 .Curve,
			           doubleValue: 1);
			//AddFeature(fc1, CurveConstruction.StartLine(10.5, 11).LineTo(20, 11).Curve,
			//		   doubleValue: 1);

			var test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 1, _doubleFieldName, 4, 0, false);
			test.CrossingMinLengthFactor = 2;
			//test.NotReportedCondition =
			//	$"G1.{_textFieldName} = 'A' OR G2.{_textFieldName} = 'A'";

			Run(test, 1000);
		}

		[Test]
		public void TestNotReportedConnnected()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1, CurveConstruction.StartLine(0, 0)
			                                 .LineTo(5, 5)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(5, 5)
			                                 .LineTo(5, 6)
			                                 .Curve,
			           doubleValue: 0, textFieldValue: "A");
			AddFeature(fc1, CurveConstruction.StartLine(5, 6)
			                                 .LineTo(0, 10)
			                                 .Curve,
			           doubleValue: 1);

			var test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 1, _doubleFieldName, 2, 0, false);
			test.NotReportedCondition =
				$"G1.{_textFieldName} = 'A' OR G2.{_textFieldName} = 'A'";

			Run(test, 1000);
		}

		[Test]
		public void TestNotReportedConnnected1()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1, CurveConstruction.StartLine(0, 0)
			                                 .LineTo(100, 0)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(0, 0.5)
			                                 .LineTo(89.5, 0.5)
			                                 .Curve,
			           doubleValue: 0, textFieldValue: "A");
			AddFeature(fc1, CurveConstruction.StartLine(90, -10)
			                                 .LineTo(90, 10)
			                                 .Curve,
			           doubleValue: 1);

			var test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 1, _doubleFieldName, 3, 1.5, false);
			test.NotReportedCondition =
				$"G1.{_textFieldName} = 'A' OR G2.{_textFieldName} = 'A'";

			Run(test, 1000);
		}

		[Test]
		public void TestLineCaps()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1, CurveConstruction.StartLine(0, 5)
			                                 .LineTo(20, 5)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(8, 6.5)
											 .LineTo(8, 10)
											 .Curve,
					   doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(3, 10)
											 .LineTo(3, 6.5)
											 .Curve,
					   doubleValue: 1);

			AddFeature(fc1, CurveConstruction.StartLine(13, 10)
											 .LineTo(13, 5.5)
											 .Curve,
					   doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(18, 4.5)
											 .LineTo(18, 10)
											 .Curve,
					   doubleValue: 1);

			var test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 1, _doubleFieldName, 1, 0, false);

			IList<QaError> errors = Run(test, 1000);
			Assert.AreEqual(8, errors.Count);

			test.UnconnectedLineCapStyle = LineCapStyle.Butt;
			// Check in QaTopoNotNear.LineEndsAdapter.RecalcFlatEnd near RecalcPart() (line # 220)
			errors = Run(test, 1000);
			Assert.AreEqual(4, errors.Count);
		}

		[Test]
		public void TestLineCaps1()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1, CurveConstruction.StartLine(2, 4.8)
			                                 .LineTo(2, 0)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(2, 5.2)
			                                 .LineTo(2, 10)
			                                 .Curve,
			           doubleValue: 1);

			var test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 1, _doubleFieldName, 1, 0, false);

			IList<QaError> errors = Run(test, 1000);
			Assert.AreEqual(2, errors.Count);

			test.UnconnectedLineCapStyle = LineCapStyle.Butt;
			errors = Run(test, 1000);
			Assert.AreEqual(0, errors.Count);
		}

		[Test]
		public void TestLineCaps2()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1, CurveConstruction.StartLine(2, 4.8)
			                                 .LineTo(2, 4.75)
			                                 .LineTo(2, 0)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(2, 5.2)
			                                 .LineTo(2, 5.25)
			                                 .LineTo(2, 10)
			                                 .Curve,
			           doubleValue: 1);

			var test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 1, _doubleFieldName, 1, 0, false);

			IList<QaError> errors = Run(test, 1000);
			Assert.AreEqual(2, errors.Count);

			test.UnconnectedLineCapStyle = LineCapStyle.Butt;
			errors = Run(test, 1000);
			Assert.AreEqual(0, errors.Count);
		}

		[Test]
		public void TestLineCapsShortSegment()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1, CurveConstruction.StartLine(2, 4.8)
			                                 .LineTo(2, 4.75)
			                                 .LineTo(2, 0)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(2, 5.2)
			                                 .LineTo(2, 6)
			                                 .Curve,
			           doubleValue: 1);

			var test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 1, _doubleFieldName, 1, 0, false);
			test.UnconnectedLineCapStyle = LineCapStyle.Butt;

			IList<QaError> errors = Run(test, 1000);
			Assert.AreEqual(1, errors.Count);
		}

		[Test]
		public void TestLineCapsAngledEnds()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1, CurveConstruction.StartLine(2, 4.8)
			                                 .LineTo(2, 4.75)
			                                 .LineTo(2, 0)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(2, 5.2)
			                                 .LineTo(2, 6)
			                                 .LineTo(12, 6)
			                                 .Curve,
			           doubleValue: 1);

			var test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 1, _doubleFieldName, 1, 0, false);
			test.UnconnectedLineCapStyle = LineCapStyle.Butt;

			IList<QaError> errors = Run(test, 1000);
			Assert.AreEqual(1, errors.Count);
		}

		[Test]
		public void TestNotNearSplit()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1,
			           CurveConstruction.StartLine(0, 0)
			                            .LineTo(4.2, 4.2)
			                            .LineTo(5.6, 5.6)
			                            .LineTo(10, 10).Curve);

			AddFeature(fc1, CurveConstruction.StartLine(5.5, 5)
			                                 .LineTo(4.7, 5)
			                                 .LineTo(4.5, 5)
			                                 .Curve);
			AddFeature(fc1, CurveConstruction.StartLine(0, 5)
			                                 .LineTo(4.5, 5)
			                                 .LineTo(0, 10)
			                                 .Curve);
			AddFeature(fc1, CurveConstruction.StartLine(5.5, 5)
			                                 .LineTo(10, 5)
			                                 .Curve);

			var test = new QaTopoNotNear(ReadOnlyTableFactory.Create(fc1), 1, 2.6);

			AssertErrors(2, Run(test, 1000));
		}

		[Test]
		public void TestNotNearJoined()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			double x0 = 0;
			double x1 = 100;
			double y0 = 0;

			var nSplits = 10;

			double dx = (x1 - x0) / nSplits;
			var dy = 0.2;

			for (var i = 0; i < nSplits; i++)
			{
				double xs = x0 + i * dx;
				double xe = xs + dx;

				double ys = y0 + dy;
				double ye = y0 + dy;

				AddFeature(fc1, CurveConstruction.StartLine(xs, ys)
				                                 .LineTo(xe, ye)
				                                 .Curve);

				ys = y0 - dy;
				ye = y0 - dy;
				AddFeature(fc1, CurveConstruction.StartLine(xs, ys)
				                                 .LineTo(xe, ye)
				                                 .Curve);
			}

			var test = new QaTopoNotNear(ReadOnlyTableFactory.Create(fc1), 1, 2.6);

			AssertErrors(20, Run(test, 1000));
		}

		[Test]
		public void TestNotNearCoincidenceTuple()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			var pts = new List<IPoint>
			          {
				          GeometryFactory.CreatePoint(0, 0),
				          GeometryFactory.CreatePoint(3.5, 3.5)
			          };

			for (var i = 1; i < pts.Count; i++)
			{
				IPolycurve part = CurveConstruction.StartLine(pts[i - 1])
				                                   .LineTo(pts[i])
				                                   .Curve;
				AddFeature(fc1, part);
				IPolycurve reverse = CurveConstruction.StartLine(pts[i])
				                                      .LineTo(pts[i - 1])
				                                      .Curve;
				AddFeature(fc1, reverse);
			}

			var test = new QaTopoNotNear(
				           ReadOnlyTableFactory.Create(fc1), 1, 2.6)
			           { AllowCoincidentSections = true };

			AssertErrors(0, Run(test, 1000));
		}

		[Test]
		public void TestNotNearCoincidenceTriple()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			var pts = new List<IPoint>
			          {
				          GeometryFactory.CreatePoint(0, 0),
				          GeometryFactory.CreatePoint(3.5, 3.5),
				          GeometryFactory.CreatePoint(8.0, 8.0),
				          GeometryFactory.CreatePoint(11.0, 11.0),
				          GeometryFactory.CreatePoint(15.0, 15.0)
			          };

			CurveConstruction all = CurveConstruction.StartLine(pts[0]);
			for (var i = 1; i < pts.Count; i++)
			{
				all.LineTo(pts[i]);
				IPolycurve part = CurveConstruction.StartLine(pts[i - 1])
				                                   .LineTo(pts[i])
				                                   .Curve;
				AddFeature(fc1, part);
				IPolycurve reverse = CurveConstruction.StartLine(pts[i])
				                                      .LineTo(pts[i - 1])
				                                      .Curve;
				AddFeature(fc1, reverse);
			}

			AddFeature(fc1, all.Curve);
			AddFeature(fc1, CurveConstruction.StartLine(0, 0.1)
			                                 .LineTo(14.9, 15)
			                                 .Curve);

			var test = new QaTopoNotNear(
				           ReadOnlyTableFactory.Create(fc1), 1, 2.6)
			           { AllowCoincidentSections = true };

			AssertErrors(5, Run(test, 1000));
		}

		[Test]
		public void TestNotNearGrid()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);
			var n = 3;
			var dx = 10.0;

			for (var ix = 0; ix < n; ix++)
			{
				for (var iy = 0; iy < n; iy++)
				{
					AddFeature(fc1,
					           CurveConstruction.StartLine(ix * dx, iy * dx)
					                            .LineTo(ix * dx, (iy + 1) * dx)
					                            .Curve);
					AddFeature(fc1,
					           CurveConstruction.StartLine(ix * dx, iy * dx)
					                            .LineTo((ix + 1) * dx, iy * dx)
					                            .Curve);
				}
			}

			AddFeature(fc1,
			           CurveConstruction.StartLine(0.5, 0.5)
			                            .LineTo(n * dx + 0.5, 0.5)
			                            .Curve);
			AddFeature(fc1,
			           CurveConstruction.StartLine(0.5 + dx, 0.5)
			                            .LineTo(0.5 + dx, n * dx + 0.5)
			                            .Curve);

			var test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 1, 2.6);

			AssertErrors(8, Run(test, 1000));

			AssertErrors(8, Run(test, 18));
		}

		[Test]
		public void TestCircular()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1, CurveConstruction.StartLine(0, 0).LineTo(5, 0.2).Curve);
			AddFeature(fc1, CurveConstruction.StartLine(5, 0.2).LineTo(10, 0).Curve);
			AddFeature(fc1, CurveConstruction.StartLine(0, 0).LineTo(10, 0).Curve);
			AddFeature(fc1, CurveConstruction.StartLine(5, 0.2).LineTo(18, 0.8).Curve);
			AddFeature(fc1, CurveConstruction.StartLine(10, 0).LineTo(18, 0).Curve);

			var test = new QaTopoNotNear(ReadOnlyTableFactory.Create(fc1), 1, 15);

			Run(test, 1000);
			//			AssertErrors(0, Run(test, 1000));
		}

		[Test]
		public void TestCircularCombined()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			const int nx = 3;
			const int ny = 3;
			const double d = 2.0;
			for (var ix = 0; ix < nx; ix++)
			{
				for (var iy = 0; iy < ny; iy++)
				{
					AddFeature(fc1,
					           CurveConstruction.StartLine(ix * d, iy * d)
					                            .LineTo((ix + 1) * d, iy * d)
					                            .Curve);
					AddFeature(fc1,
					           CurveConstruction.StartLine(ix * d, iy * d)
					                            .LineTo(ix * d, (iy + 1) * d)
					                            .Curve);
				}
			}

			AddFeature(fc1,
			           CurveConstruction.StartLine(0, ny * d)
			                            .LineTo(0, ny * d + 100)
			                            .Curve);
			AddFeature(fc1,
			           CurveConstruction.StartLine(d, ny * d)
			                            .LineTo(d, ny * d + 100)
			                            .Curve);
			var test = new QaTopoNotNear(ReadOnlyTableFactory.Create(fc1), 1.5, 15);

			Run(test, 1000);
			//			AssertErrors(0, Run(test, 1000));
		}

		[Test]
		public void TestCircular2()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1, CurveConstruction.StartLine(-10, 0)
			                                 .LineTo(-10, 0.4)
			                                 .LineTo(-14, 0.4)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(-14, 0.4)
			                                 .LineTo(-12, 0.3)
			                                 .LineTo(-10, 0)
			                                 .Curve,
			           doubleValue: 1);

			var test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 2, _doubleFieldName,
				10, 0, false);

			AssertErrors(1, Run(test, 1000));
		}

		[Test]
		[Category(Commons.Test.TestCategory.FixMe)]
		public void TestCircular3()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1, CurveConstruction.StartLine(-10, 0)
			                                 .LineTo(-10, 0.9)
			                                 .LineTo(-14, 0.9)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(-14, 0.9)
			                                 .LineTo(-14, -0.9)
			                                 .LineTo(-10, -0.9)
			                                 .LineTo(-10, 0)
			                                 .Curve,
			           doubleValue: 1);

			var test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 2, _doubleFieldName,
				10, 0, false);

			AssertErrors(1, Run(test, 1000));
		}

		[Test]
		[Category(Commons.Test.TestCategory.FixMe)]
		public void TestAuraConnected()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1, CurveConstruction.StartLine(0, 100)
			                                 .LineTo(0, 10)
			                                 .Curve,
			           doubleValue: 10); // 1
			AddFeature(fc1, CurveConstruction.StartLine(0, 10)
			                                 .LineTo(0, -10)
			                                 .Curve,
			           doubleValue: 10); // 2
			AddFeature(fc1, CurveConstruction.StartLine(0, 10)
			                                 .LineTo(0, -10)
			                                 .Curve,
			           doubleValue: 0); // 3
			AddFeature(fc1, CurveConstruction.StartLine(0, -10)
			                                 .LineTo(0, -100)
			                                 .Curve,
			           doubleValue: 10); // 4

			AddFeature(fc1, CurveConstruction.StartLine(-100, 0)
			                                 .LineTo(-8, 0)
			                                 .Curve,
			           doubleValue: 10); // 5
			AddFeature(fc1, CurveConstruction.StartLine(-8, 0)
			                                 .LineTo(-2, 0)
			                                 .Curve,
			           doubleValue: 0); // 6
			AddFeature(fc1, CurveConstruction.StartLine(-8, 0)
			                                 .LineTo(-2, 0)
			                                 .Curve,
			           doubleValue: 0); // 7
			AddFeature(fc1, CurveConstruction.StartLine(-2, 0)
			                                 .LineTo(100, 0)
			                                 .Curve,
			           doubleValue: 10); // 8

			var test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 10, _doubleFieldName,
				10, 0, false);
			test.CrossingMinLengthFactor = 3;
			test.SetConstraint(0, "ObjectId in (1,2,4,8)");

			AssertErrors(0, Run(test, 1000));
		}

		[Test]
		public void TestWithAura()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1,
			           CurveConstruction.StartLine(0, 0)
			                            .LineTo(0, 7)
			                            .Curve,
			           doubleValue: 1);
			AddFeature(fc1,
			           CurveConstruction.StartLine(-10, 0)
			                            .LineTo(0, 0)
			                            .LineTo(10, 0)
			                            .Curve,
			           doubleValue: 1);

			var test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 2, _doubleFieldName,
				10, 0, false);

			AssertErrors(0, Run(test, 1000));
		}

		[Test]
		public void TestWithAuraCoincident()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1,
			           CurveConstruction.StartLine(0, 0)
			                            .LineTo(5, 5)
			                            .LineTo(9, 9)
			                            .Curve,
			           doubleValue: 1);
			AddFeature(fc1,
			           CurveConstruction.StartLine(0, 0)
			                            .LineTo(5, 5)
			                            .LineTo(9, 9)
			                            .Curve,
			           doubleValue: 1);

			var test = new QaTopoNotNear(
				           ReadOnlyTableFactory.Create(fc1), 2, _doubleFieldName,
				           10, 0, false)
			           { AllowCoincidentSections = true };

			AssertErrors(0, Run(test, 1000));
		}

		[Test]
		[Category(Commons.Test.TestCategory.FixMe)]
		public void TestWithAuraMultiparts()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1, CurveConstruction.StartLine(0, 0)
			                                 .LineTo(0, 0.2)
			                                 .LineTo(0, 0.3)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(0, 0.3)
			                                 .LineTo(0, 0.5)
			                                 .LineTo(0, 0.6)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(0, 0.6)
			                                 .LineTo(0, 0.8)
			                                 .LineTo(0, 0.9)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(0, 0.9)
			                                 .LineTo(0, 1.2)
			                                 .Curve,
			           doubleValue: 1);
			AddFeature(fc1, CurveConstruction.StartLine(-10, 0)
			                                 .LineTo(-0.5, 0)
			                                 .LineTo(0, 0)
			                                 .LineTo(0.4, 0)
			                                 .LineTo(10, 0)
			                                 .Curve,
			           doubleValue: 1);

			var test = new QaTopoNotNear(
				           ReadOnlyTableFactory.Create(fc1), 2, _doubleFieldName,
				           10, 0, false)
			           { AllowCoincidentSections = true };

			AssertErrors(0, Run(test, 1000));
		}

		[Test]
		public void CanDetectInconsistentLineEnd()
		{
			for (var angle = 0; angle <= 360; angle = angle + 15)
			{
				bool consistent = ! QaTopoNotNear.HasInconsistentLineEnd(
					                  lineWidth: 10,
					                  segmentDirection0: 0,
					                  segmentDirection1: MathUtils.ToRadians(angle),
					                  segmentLength: 10);

				Console.WriteLine($@"{angle}: consistent end: {consistent}");
			}

			// observed case, looks ok visually but is actually inconsistent
			Assert.IsTrue(QaTopoNotNear.HasInconsistentLineEnd(27, 2.891, -2.670, 17.7));

			Assert.IsFalse(
				QaTopoNotNear.HasInconsistentLineEnd(100, 0, MathUtils.ToRadians(45), 100));
			Assert.IsFalse(
				QaTopoNotNear.HasInconsistentLineEnd(100, 0, MathUtils.ToRadians(90), 100));
			Assert.IsTrue(
				QaTopoNotNear.HasInconsistentLineEnd(100, 0, MathUtils.ToRadians(90), 90));

			// acute angles:
			Assert.IsTrue(
				QaTopoNotNear.HasInconsistentLineEnd(100, 0, MathUtils.ToRadians(91), 100));
			Assert.IsTrue(
				QaTopoNotNear.HasInconsistentLineEnd(100, MathUtils.ToRadians(270),
				                                     MathUtils.ToRadians(1), 100));
			Assert.IsFalse(
				QaTopoNotNear.HasInconsistentLineEnd(100, 0, MathUtils.ToRadians(91), 110));

			Assert.IsFalse(
				QaTopoNotNear.HasInconsistentLineEnd(100, Math.PI, 1.1 * Math.PI, 100));

			Assert.IsFalse(QaTopoNotNear.HasInconsistentLineEnd(100, 0, 0, 1));

			Assert.IsFalse(QaTopoNotNear.HasInconsistentLineEnd(
				               lineWidth: 10,
				               segmentDirection0: 0,
				               segmentDirection1: MathUtils.ToRadians(90),
				               segmentLength: 10));

			Assert.IsTrue(QaTopoNotNear.HasInconsistentLineEnd(
				              lineWidth: 10,
				              segmentDirection0: 0,
				              segmentDirection1: MathUtils.ToRadians(91),
				              segmentLength: 10));
		}

		[Test]
		public void TestSegmentPair()
		{
			IPolycurve line1 = CurveConstruction.StartLine(0, 0).LineTo(10, 0).Curve;
			ISegment seg1 = ((ISegmentCollection) line1).Segment[0];
			SegmentProxy proxy1 = new AoSegmentProxy(seg1, 0, 0);
			//			SegmentHull hull1Asym = new SegmentHull(proxy1, 1, 2, new RoundCap(), new RoundCap());
			var hull1Asym = new SegmentHull(proxy1, 2.01, 2, new RoundCap(), new RoundCap());
			var hull1Sym = new SegmentHull(proxy1, 2, new RoundCap(), new RoundCap());

			IPolycurve line2 = CurveConstruction.StartLine(4, -7).LineTo(8, 7).Curve;
			ISegment seg2 = ((ISegmentCollection) line2).Segment[0];
			SegmentProxy proxy2 = new AoSegmentProxy(seg2, 0, 0);
			var hull2 = new SegmentHull(proxy2, 1, new RoundCap(), new RoundCap());

			var pairAsym = new SegmentPair2D(hull1Asym, hull2);

			IList<double[]> limitsAsym;
			pairAsym.CutCurveHull(0, out limitsAsym, out _, out _, out _);

			var pairSym = new SegmentPair2D(hull1Sym, hull2);
			pairSym.CutCurveHull(0, out _, out _, out _, out _);

			IPolycurve lineP1 =
				CurveConstruction.StartLine(0, 0).LineTo(1.001 * limitsAsym[0][0] * 10, 0).Curve;
			ISegment segP1 = ((ISegmentCollection) lineP1).Segment[0];
			SegmentProxy proxyP1 = new AoSegmentProxy(segP1, 0, 0);
			var hullP1 = new SegmentHull(proxyP1, 1, new RoundCap(), new RoundCap());

			var pairP1 = new SegmentPair2D(hullP1, hull2);

			pairP1.CutCurveHull(0, out _, out _, out _, out _);
		}

		[Test]
		public void TestCutLine_LineSimple()
		{
			var y = new LineHullPart(new Pnt2D(4, 4), new Pnt2D(7, 7));
			{
				var x =
					new HullLineSimple { Lin = new Lin2D(new Pnt2D(0, 5), new Pnt2D(10, 5)) };
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsTrue(y.Cut(x, ref tMin, ref tMax));
				Assert.AreEqual(0.5, tMin);
				Assert.AreEqual(0.5, tMax);
			}
			{
				var x =
					new HullLineSimple { Lin = new Lin2D(new Pnt2D(0, 3.9), new Pnt2D(10, 3.9)) };
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsFalse(y.Cut(x, ref tMin, ref tMax));
			}
			{
				var x =
					new HullLineSimple { Lin = new Lin2D(new Pnt2D(0, 7.1), new Pnt2D(10, 7.1)) };
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsFalse(y.Cut(x, ref tMin, ref tMax));
			}
			{
				// parallel intersect
				var x =
					new HullLineSimple { Lin = new Lin2D(new Pnt2D(5, 5), new Pnt2D(9, 9)) };
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsTrue(y.Cut(x, ref tMin, ref tMax));
				Assert.AreEqual(-0.25, tMin);
				Assert.AreEqual(0.5, tMax);
			}
			{
				// parallel no intersect
				var x =
					new HullLineSimple { Lin = new Lin2D(new Pnt2D(5.1, 5), new Pnt2D(9.1, 9)) };
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsFalse(y.Cut(x, ref tMin, ref tMax));
			}
		}

		[Test]
		public void TestCutLine_LineLine()
		{
			var y = new LineHullPart(new Pnt2D(4, 4), new Pnt2D(7, 7));
			{
				var x = new HullLineLine
				        {
					        Lin = new Lin2D(new Pnt2D(0, 0), new Pnt2D(10, 0)),
					        EndPart = new Lin2D(new Pnt2D(0, 0), new Pnt2D(0, 5))
				        };
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsTrue(y.Cut(x, ref tMin, ref tMax));
				Assert.AreEqual(0.4, tMin);
				Assert.AreEqual(0.5, tMax);
			}
			{
				var x = new HullLineLine
				        {
					        Lin = new Lin2D(new Pnt2D(0, 0), new Pnt2D(10, 0)),
					        EndPart = new Lin2D(new Pnt2D(0, 0), new Pnt2D(0, 3.9))
				        };
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsFalse(y.Cut(x, ref tMin, ref tMax));
			}
			{
				var x = new HullLineLine
				        {
					        Lin = new Lin2D(new Pnt2D(10, 0), new Pnt2D(0, 0)),
					        EndPart = new Lin2D(new Pnt2D(0, 0), new Pnt2D(0, 5))
				        };
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsTrue(y.Cut(x, ref tMin, ref tMax));
				Assert.AreEqual(0.5, tMin);
				Assert.AreEqual(0.6, tMax);
			}
			{
				var x = new HullLineLine
				        {
					        Lin = new Lin2D(new Pnt2D(10, 0), new Pnt2D(20, 0)),
					        EndPart = new Lin2D(new Pnt2D(0, 0), new Pnt2D(0, 5))
				        };
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsTrue(y.Cut(x, ref tMin, ref tMax));
				Assert.AreEqual(-0.6, tMin);
				Assert.AreEqual(-0.5, tMax);
			}
			{
				var x = new HullLineLine
				        {
					        Lin = new Lin2D(new Pnt2D(0, 0), new Pnt2D(10, 0)),
					        EndPart = new Lin2D(new Pnt2D(0, -1.1), new Pnt2D(0, 3.9))
				        };
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsFalse(y.Cut(x, ref tMin, ref tMax));
			}
		}

		[Test]
		public void TestCutLine_LineArc()
		{
			var y = new LineHullPart(new Pnt2D(4, 4), new Pnt2D(7, 7));
			{
				var x = new HullLineArc
				        {
					        Lin = new Lin2D(new Pnt2D(0, 0), new Pnt2D(10, 0)),
					        Radius = 5,
					        StartDirection = -Math.PI / 2,
					        Angle = Math.PI
				        };
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsTrue(y.Cut(x, ref tMin, ref tMax));
				Assert.AreEqual(0.1, tMin); // exactly 0.1 due to Pythagoras 3^2 + 4^2 = 5^2
				Assert.AreEqual(0.5, tMax);
			}
			{
				var x = new HullLineArc
				        {
					        Lin = new Lin2D(new Pnt2D(0, 0), new Pnt2D(10, 0)),
					        Radius = 3.9,
					        StartDirection = -Math.PI / 2,
					        Angle = Math.PI
				        };
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsFalse(y.Cut(x, ref tMin, ref tMax));
			}
			{
				var x = new HullLineArc
				        {
					        Lin = new Lin2D(new Pnt2D(0, 5), new Pnt2D(10, 5)),
					        Radius = 5,
					        StartDirection = -Math.PI / 2,
					        Angle = Math.PI / 4
				        };
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsFalse(y.Cut(x, ref tMin, ref tMax));
			}
			{
				var x = new HullLineArc
				        {
					        Lin = new Lin2D(new Pnt2D(0, 5), new Pnt2D(10, 5)),
					        Radius = 5,
					        StartDirection = -Math.PI / 2,
					        Angle = Math.PI / 2
				        };
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsTrue(y.Cut(x, ref tMin, ref tMax));

				Assert.IsTrue(Math.Abs(-0.089 - tMin) < 0.001);
				Assert.AreEqual(0.0, tMax);
			}

			{
				var x = new HullLineArc
				        {
					        Lin = new Lin2D(new Pnt2D(0, 5), new Pnt2D(10, 5)),
					        Radius = 0.8,
					        StartDirection = -Math.PI / 2,
					        Angle = Math.PI
				        };
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsTrue(y.Cut(x, ref tMin, ref tMax));

				Assert.IsTrue(Math.Abs(0.387 - tMin) < 0.001);
				Assert.AreEqual(0.58, tMax);
			}
			{
				var x = new HullLineArc
				        {
					        Lin = new Lin2D(new Pnt2D(0, 5), new Pnt2D(10, 5)),
					        Radius = 0.8,
					        StartDirection = -Math.PI / 2,
					        Angle = Math.PI / 2
				        };
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsTrue(y.Cut(x, ref tMin, ref tMax));

				Assert.IsTrue(Math.Abs(0.387 - tMin) < 0.001);
				Assert.AreEqual(0.42, tMax);
			}

			{
				var x = new HullLineArc
				        {
					        Lin = new Lin2D(new Pnt2D(0, 5), new Pnt2D(10, 5)),
					        Radius = 0.8,
					        StartDirection = Math.PI / 2,
					        Angle = Math.PI
				        };
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsTrue(y.Cut(x, ref tMin, ref tMax));

				Assert.AreEqual(0.42, tMin);
				Assert.IsTrue(Math.Abs(0.613 - tMax) < 0.001);
			}
			{
				var x = new HullLineArc
				        {
					        Lin = new Lin2D(new Pnt2D(0, 5), new Pnt2D(10, 5)),
					        Radius = 0.8,
					        StartDirection = Math.PI / 2,
					        Angle = Math.PI / 2
				        };
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsTrue(y.Cut(x, ref tMin, ref tMax));

				Assert.AreEqual(0.58, tMin);
				Assert.IsTrue(Math.Abs(0.613 - tMax) < 0.001);
			}
		}

		[Test]
		public void TestCutArc_LineSimple()
		{
			var y = new CircleHullPart(new Pnt2D(4, 4), 5)
			        {
				        StartDirection = -Math.PI / 2,
				        Angle = 3 * Math.PI / 4
			        };
			{
				var x =
					new HullLineSimple { Lin = new Lin2D(new Pnt2D(0, 7), new Pnt2D(10, 7)) };
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsTrue(y.Cut(x, ref tMin, ref tMax));
				Assert.AreEqual(0.8, tMin);
				Assert.AreEqual(0.8, tMax);
			}
			{
				var x =
					new HullLineSimple { Lin = new Lin2D(new Pnt2D(8, 0), new Pnt2D(8, 10)) };
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsTrue(y.Cut(x, ref tMin, ref tMax));
				Assert.AreEqual(0.1, tMin);
				Assert.AreEqual(0.7, tMax);
			}
			{
				var x =
					new HullLineSimple { Lin = new Lin2D(new Pnt2D(0, 8), new Pnt2D(10, 8)) };
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsFalse(y.Cut(x, ref tMin, ref tMax));
			}
			{
				var x =
					new HullLineSimple { Lin = new Lin2D(new Pnt2D(0, -1.1), new Pnt2D(10, -1.1)) };
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsFalse(y.Cut(x, ref tMin, ref tMax));
			}
		}

		[Test]
		public void TestCutArc_LineLine()
		{
			var y = new CircleHullPart(new Pnt2D(4, 4), 5)
			        {
				        StartDirection = -Math.PI / 2,
				        Angle = 3 * Math.PI / 4
			        };
			{
				var x =
					new HullLineLine
					{
						Lin = new Lin2D(new Pnt2D(0, 0), new Pnt2D(10, 0)),
						EndPart = new Lin2D(new Pnt2D(0, 0), new Pnt2D(0, 1))
					};
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsTrue(y.Cut(x, ref tMin, ref tMax));
				Assert.AreEqual(0.7, tMin);
				Assert.AreEqual(0.8, tMax);
			}
			{
				var x =
					new HullLineLine
					{
						Lin = new Lin2D(new Pnt2D(0, 7), new Pnt2D(10, 7)),
						EndPart = new Lin2D(new Pnt2D(0, 0), new Pnt2D(0, 1))
					};
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsTrue(y.Cut(x, ref tMin, ref tMax));
				Assert.IsTrue(Math.Abs(0.754 - tMin) < 0.001);
				Assert.AreEqual(0.8, tMax);
			}
			{
				var x =
					new HullLineLine
					{
						Lin = new Lin2D(new Pnt2D(0, 6), new Pnt2D(10, 6)),
						EndPart = new Lin2D(new Pnt2D(0, 1), new Pnt2D(0, 2))
					};
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsTrue(y.Cut(x, ref tMin, ref tMax));
				Assert.IsTrue(Math.Abs(0.754 - tMin) < 0.001);
				Assert.AreEqual(0.8, tMax);
			}
			{
				var x =
					new HullLineLine
					{
						Lin = new Lin2D(new Pnt2D(0, 4), new Pnt2D(10, 4)),
						EndPart = new Lin2D(new Pnt2D(0, -1), new Pnt2D(0, 1))
					};
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsTrue(y.Cut(x, ref tMin, ref tMax));
				Assert.IsTrue(Math.Abs(0.890 - tMin) < 0.001);
				Assert.AreEqual(0.9, tMax);
			}
			{
				var x =
					new HullLineLine
					{
						Lin = new Lin2D(new Pnt2D(0, 7.6), new Pnt2D(10, 7.6)),
						EndPart = new Lin2D(new Pnt2D(0, 0), new Pnt2D(0, 1))
					};
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsFalse(y.Cut(x, ref tMin, ref tMax));
			}
		}

		[Test]
		public void TestCutArc_LineArc()
		{
			var y = new CircleHullPart(new Pnt2D(4, 4), 5)
			        {
				        StartDirection = -Math.PI / 2,
				        Angle = 3 * Math.PI / 4
			        };
			{
				var x =
					new HullLineArc
					{
						Lin = new Lin2D(new Pnt2D(0, 4), new Pnt2D(10, 4)),
						Radius = 1,
						Angle = 2 * Math.PI
					};
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsTrue(y.Cut(x, ref tMin, ref tMax));
				Assert.AreEqual(0.8, tMin);
				Assert.AreEqual(1.0, tMax);
			}
			{
				var x =
					new HullLineArc
					{
						Lin = new Lin2D(new Pnt2D(0, 4), new Pnt2D(10, 4)),
						Radius = 1,
						StartDirection = -Math.PI / 2,
						Angle = Math.PI
					};
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsTrue(y.Cut(x, ref tMin, ref tMax));
				Assert.AreEqual(0.8, tMin);
				Assert.IsTrue(Math.Abs(0.890 - tMax) < 0.001);
			}
			{
				var x =
					new HullLineArc
					{
						Lin = new Lin2D(new Pnt2D(0, 4), new Pnt2D(10, 4)),
						Radius = 1,
						StartDirection = Math.PI / 2,
						Angle = Math.PI
					};
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsTrue(y.Cut(x, ref tMin, ref tMax));
				Assert.IsTrue(Math.Abs(0.890 - tMin) < 0.001);
				Assert.AreEqual(1.0, tMax);
			}
			{
				var x =
					new HullLineArc
					{
						Lin = new Lin2D(new Pnt2D(0, -1), new Pnt2D(10, -1)),
						Radius = 1,
						StartDirection = -Math.PI / 2,
						Angle = Math.PI
					};
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsTrue(y.Cut(x, ref tMin, ref tMax));
				Assert.AreEqual(0.3, tMin);
				Assert.AreEqual(0.7, tMax);
			}
			{
				var x =
					new HullLineArc
					{
						Lin = new Lin2D(new Pnt2D(0, -1), new Pnt2D(10, -1)),
						Radius = 1,
						StartDirection = Math.PI / 2,
						Angle = Math.PI
					};
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsTrue(y.Cut(x, ref tMin, ref tMax));
				Assert.AreEqual(0.5, tMin);
				Assert.IsTrue(Math.Abs(0.732 - tMax) < 0.001);
			}
			{
				var x =
					new HullLineArc
					{
						Lin = new Lin2D(new Pnt2D(0, 8.6), new Pnt2D(10, 8.6)),
						Radius = 1,
						StartDirection = Math.PI / 2,
						Angle = Math.PI
					};
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsFalse(y.Cut(x, ref tMin, ref tMax));
			}
			{
				var x =
					new HullLineArc
					{
						Lin = new Lin2D(new Pnt2D(0, 4), new Pnt2D(10, 4)),
						Radius = 7,
						StartDirection = -Math.PI / 2,
						Angle = Math.PI
					};
				double tMin = double.MaxValue;
				double tMax = double.MinValue;
				Assert.IsTrue(y.Cut(x, ref tMin, ref tMax));
				Assert.IsTrue(Math.Abs(-0.090 - tMin) < 0.001);
				Assert.AreEqual(0.2, tMax);
			}
		}

		[Test]
		public void TestRightSide()
		{
			IFeatureClass fc1;
			CreateFeatureClasses(out fc1, out _);

			AddFeature(fc1, CurveConstruction.StartLine(1, 1)
			                                 .LineTo(0, 1)
			                                 .LineTo(0, 0)
			                                 .Curve);

			AddFeature(fc1, CurveConstruction.StartLine(0, 2)
			                                 .LineTo(0, 1.1)
			                                 .LineTo(1, 1.1)
			                                 .Curve);

			var test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 1, "0.1", 0.5, 0.5, false);

			IList<QaError> errors = Run(test, 1000);
			Assert.AreEqual(2, errors.Count);

			test.RightSideNears = new[] { "0.01" };
			errors = Run(test, 1000);
			Assert.AreEqual(0, errors.Count);

			test = new QaTopoNotNear(
				ReadOnlyTableFactory.Create(fc1), 1, "0.01", 0.5, 0.5, false);
			errors = Run(test, 1000);
			Assert.AreEqual(0, errors.Count);

			test.RightSideNears = new[] { "0.10" };
			errors = Run(test, 1000);
			Assert.AreEqual(2, errors.Count);
		}

		[Test]
		[Category(Commons.Test.TestCategory.FixMe)]
		public void TestReference()
		{
			IFeatureClass fc1;
			IFeatureClass fc2;
			CreateFeatureClasses(out fc1, out fc2);

			AddFeature(fc1, CurveConstruction.StartLine(0, 0)
			                                 .LineTo(0, 1)
			                                 .LineTo(1, 1)
			                                 .Curve);
			AddFeature(fc1, CurveConstruction.StartLine(1, 1)
			                                 .LineTo(5, 1)
			                                 .Curve);

			AddFeature(fc2, CurveConstruction.StartLine(1.1, -5)
			                                 .LineTo(1.1, 1)
			                                 .Curve);
			AddFeature(fc2, CurveConstruction.StartLine(1.1, 1)
			                                 .LineTo(1.1, 5)
			                                 .Curve);

			{
				var test = new QaTopoNotNear(
					ReadOnlyTableFactory.Create(fc1), ReadOnlyTableFactory.Create(fc2), 1,
					"IIF(objectId = 1, 0.8, 0)", "0", 0.1,
					0.1, false);
				IList<QaError> errors = Run(test, 1000);
				Assert.AreEqual(1, errors.Count);
			}
			{
				var test = new QaTopoNotNear(
					ReadOnlyTableFactory.Create(fc1), ReadOnlyTableFactory.Create(fc2), 1,
					"IIF(objectId = 1, 0.8, 0)", "0", 0.1,
					0.1, false);
				test.SetConstraint(0, "ObjectId = 1");
				// test.UnconnectedLineCapStyle = LineCapStyle.Round;
				IList<QaError> errors = Run(test, 1000);
				Assert.AreEqual(1, errors.Count);
			}
			{
				var test = new QaTopoNotNear(
					ReadOnlyTableFactory.Create(fc1), ReadOnlyTableFactory.Create(fc2), 1,
					"IIF(objectId = 1, 0.8, 0)", "0", 0.1,
					0.1, false);
				test.SetConstraint(0, "ObjectId = 1");
				test.UnconnectedLineCapStyle = LineCapStyle.Butt;
				//test.JunctionIsEndExpression = "n0:true; n1:ObjectId=2; n0 = 2 AND n1 = 1";
				//test.EndCapStyle = LineCapStyle.Butt;

				IList<QaError> errors = Run(test, 1000);
				Assert.AreEqual(0, errors.Count);
			}
			{
				var test = new QaTopoNotNear(
					ReadOnlyTableFactory.Create(fc1), ReadOnlyTableFactory.Create(fc2), 1,
					"IIF(objectId = 1, 0.8, 0)", "0", 0.1,
					0.1, false);
				//test.SetConstraint(0, "ObjectId = 1");
				//test.UnconnectedLineCapStyle = LineCapStyle.Butt;
				test.JunctionIsEndExpression = "true;n0:true; n1:ObjectId=2; n0 = 2 AND n1 = 1";
				test.EndCapStyle = LineCapStyle.Butt;

				IList<QaError> errors = Run(test, 1000);
				Assert.AreEqual(0, errors.Count);
			}
		}

		[NotNull]
		private static IList<QaError> Run([NotNull] ITest test, double? tileSize = null)
		{
			Console.WriteLine(@"Tile size: {0}",
			                  tileSize == null ? "<null>" : tileSize.ToString());
			const string newLine = "\n";
			// r# unit test output adds 2 lines for Environment.NewLine
			Console.Write(newLine);

			QaTestRunnerBase runner = tileSize == null
				                          ? (QaTestRunnerBase) new QaTestRunner(test)
				                          : new QaContainerTestRunner(tileSize.Value, test)
				                            {
					                            KeepGeometry = true
				                            };

			runner.Execute();

			return runner.Errors;
		}

		private static void AssertErrors(
			int expectedErrorCount,
			[NotNull] ICollection<QaError> errors,
			[NotNull] params Predicate<QaError>[] expectedErrorPredicates)
		{
			Assert.AreEqual(expectedErrorCount, errors.Count);

			var unmatched = new List<int>();

			for (var i = 0; i < expectedErrorPredicates.Length; i++)
			{
				Predicate<QaError> predicate = expectedErrorPredicates[i];

				bool matched = errors.Any(error => predicate(error));

				if (! matched)
				{
					unmatched.Add(i);
				}
			}

			if (unmatched.Count > 0)
			{
				Assert.Fail("Unmatched predicate index(es): {0}",
				            StringUtils.Concatenate(unmatched, "; "));
			}
		}

		[NotNull]
		private static ISpatialReference CreateLV95()
		{
			ISpatialReference result = SpatialReferenceUtils.CreateSpatialReference
				((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);
			SpatialReferenceUtils.SetXYDomain(result, -10000, -10000, 10000, 10000,
			                                  0.0001, _xyTolerance);
			return result;
		}

		private static void AddFeature([NotNull] IFeatureClass featureClass,
		                               [NotNull] IGeometry geometry,
		                               [CanBeNull] string stateId = null,
		                               [CanBeNull] string textFieldValue = null,
		                               double? doubleValue = null,
		                               DateTime? dateValue = null)
		{
			IFeature feature = featureClass.CreateFeature();
			feature.Shape = geometry;

			if (stateId != null)
			{
				SetValue(feature, _stateIdFieldName, stateId);
			}

			if (textFieldValue != null)
			{
				SetValue(feature, _textFieldName, textFieldValue);
			}

			if (doubleValue != null)
			{
				SetValue(feature, _doubleFieldName, doubleValue.Value);
			}

			if (dateValue != null)
			{
				SetValue(feature, _dateFieldName, dateValue.Value);
			}

			feature.Store();
		}

		private static void SetValue([NotNull] IRow row,
		                             [NotNull] string fieldName,
		                             [CanBeNull] object value)
		{
			int index = row.Fields.FindField(fieldName);
			Assert.True(index >= 0);

			row.Value[index] = value ?? DBNull.Value;
		}

		private void CreateFeatureClasses([NotNull] out IFeatureClass fcLine1,
		                                  [NotNull] out IFeatureClass fcLine2)
		{
			Thread.Sleep(10);
			// make sure that TickCount is unique for each call (increase is non-continuous)
			int ticks = Environment.TickCount;

			fcLine1 = CreateFeatureClass(string.Format("l1_{0}", ticks),
			                             esriGeometryType.esriGeometryPolyline);
			fcLine2 = CreateFeatureClass(string.Format("l2_{0}", ticks),
			                             esriGeometryType.esriGeometryPolyline);
		}

		private IFeatureClass CreateFeatureClass([NotNull] string name,
		                                         esriGeometryType geometryType)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", geometryType, _spatialReference, 1000));

			fields.AddField(FieldUtils.CreateTextField(_stateIdFieldName, 100));

			fields.AddField(FieldUtils.CreateTextField(_textFieldName, 200));
			fields.AddField(FieldUtils.CreateDoubleField(_doubleFieldName));
			fields.AddField(FieldUtils.CreateDateField(_dateFieldName));

			return DatasetUtils.CreateSimpleFeatureClass(
				_testWs, name, fields);
		}
	}
}

using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaWithinZRangeTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _testWs;

		private int _featureClassNumber;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();

			_testWs = TestWorkspaceUtils.CreateTestFgdbWorkspace("TestZRange");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void TestPoints()
		{
			IFeatureClass featureClass =
				TestWorkspaceUtils.CreateSimpleFeatureClass(
					_testWs, "points",
					null,
					esriGeometryType.esriGeometryPoint,
					esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, 0d,
					true);

			IFeature row1 = featureClass.CreateFeature();
			row1.Shape = GeometryFactory.CreatePoint(2000001, 1000000, 0); //below
			row1.Store();
			IFeature row2 = featureClass.CreateFeature();
			row2.Shape = GeometryFactory.CreatePoint(2000002, 1000000, 100); //border
			row2.Store();
			IFeature row3 = featureClass.CreateFeature();
			row3.Shape = GeometryFactory.CreatePoint(2000003, 1000000, 200); //inside
			row3.Store();
			IFeature row4 = featureClass.CreateFeature();
			row4.Shape = GeometryFactory.CreatePoint(2000004, 1000000, 300); //border
			row4.Store();
			IFeature row5 = featureClass.CreateFeature();
			row5.Shape = GeometryFactory.CreatePoint(2000005, 1000000, 400); //above
			row5.Store();

			RunTest(featureClass, 100, 300, 2);
		}

		[Test]
		public void TestMultipoint()
		{
			IFeatureClass featureClass =
				TestWorkspaceUtils.CreateSimpleFeatureClass(
					_testWs, "multipoints",
					null,
					esriGeometryType.esriGeometryMultipoint,
					esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, 0d,
					true);

			var points = new List<IPoint>
			             {
				             GeometryFactory.CreatePoint(2000001, 1000000, 0),
				             GeometryFactory.CreatePoint(2000002, 1000000, 100),
				             GeometryFactory.CreatePoint(2000003, 1000000, 200),
				             GeometryFactory.CreatePoint(2000004, 1000000, 300),
				             GeometryFactory.CreatePoint(2000005, 1000000, 400)
			             };

			IFeature row1 = featureClass.CreateFeature();
			row1.Shape = GeometryFactory.CreateMultipoint(points);
			row1.Store();

			IList<IGeometry> results = RunTest(featureClass, 100, 300, 2);

			var errors1 = (IMultipoint) results[0];
			Assert.AreEqual(((IPointCollection) errors1).PointCount, 1);

			var errors2 = (IMultipoint) results[1];
			Assert.AreEqual(((IPointCollection) errors2).PointCount, 1);

			IFeature row2 = featureClass.CreateFeature();
			row2.Shape = GeometryFactory.CreateMultipoint(points);
			row2.Store();

			RunTest(featureClass, 100, 300, 4);
		}

		[Test]
		public void TestSegmentsWholeOnZMax()
		{
			var expectedErrorGeometries = new List<IPolyline>();

			IPolyline segment = GeometryFactory.CreatePolyline(2000000, 1000000, 300,
			                                                   2000000, 1001000, 300);
			TestPolycurve(segment, expectedErrorGeometries, 100, 300);
		}

		[Test]
		public void TestSegmentsWholeOnZMin()
		{
			var expectedErrorGeometries = new List<IPolyline>();

			IPolyline segment = GeometryFactory.CreatePolyline(2000000, 1000000, 100,
			                                                   2000000, 1001000, 100);
			TestPolycurve(segment, expectedErrorGeometries, 100, 300);
		}

		[Test]
		public void TestSegmentsWholeBelow()
		{
			var expectedErrorGeometries = new List<IPolyline>();

			IPolyline segment = GeometryFactory.CreatePolyline(2000000, 1000000, 0,
			                                                   2000000, 1001000, 0);
			expectedErrorGeometries.Add(segment);
			TestPolycurve(segment, expectedErrorGeometries, 100, 300);
		}

		[Test]
		public void TestSegmentsWholeAbove()
		{
			var expectedErrorGeometries = new List<IPolyline>();

			IPolyline segment = GeometryFactory.CreatePolyline(2000000, 1000000, 400,
			                                                   2000000, 1001000, 400);
			expectedErrorGeometries.Add(segment);
			TestPolycurve(segment, expectedErrorGeometries, 100, 300);
		}

		[Test]
		public void TestSegmentsFirstAbove()
		{
			var expectedErrorGeometries = new List<IPolyline>();

			IPolyline segment = GeometryFactory.CreatePolyline(2000000, 1000000, 200,
			                                                   2001000, 1001000, 400);
			IPolyline errorSegment1 = GeometryFactory.CreatePolyline(2000500, 1000500, 300,
			                                                         2001000, 1001000, 400);
			expectedErrorGeometries.Add(errorSegment1);
			TestPolycurve(segment, expectedErrorGeometries, 100, 300);
		}

		[Test]
		public void TestSegmentsSecondAbove()
		{
			var expectedErrorGeometries = new List<IPolyline>();

			IPolyline segment = GeometryFactory.CreatePolyline(2000000, 1000000, 400,
			                                                   2001000, 1001000, 275);
			IPolyline errorSegment1 = GeometryFactory.CreatePolyline(2000000, 1000000, 400,
			                                                         2000800, 1000800, 300);
			expectedErrorGeometries.Add(errorSegment1);
			TestPolycurve(segment, expectedErrorGeometries, 100, 300);
		}

		[Test]
		public void TestSegmentsFirstAboveByTinyValue()
		{
			var expectedErrorGeometries = new List<IPolyline>();

			IPolyline segment = GeometryFactory.CreatePolyline(1, 1, 1000.01,
			                                                   0, 0, 0);
			const bool dontSimplify = true;
			IPolyline errorSegment1 = GeometryFactory.CreatePolyline(
				1.00000191107392, 1.00000191479921, 1000.01,
				0.999991911154811, 0.999991914880027, 1000,
				dontSimplify);
			expectedErrorGeometries.Add(errorSegment1);
			TestPolycurve(segment, expectedErrorGeometries, 0, 1000);
		}

		[Test]
		public void TestSegmentsSecondAboveByTinyValue()
		{
			var expectedErrorGeometries = new List<IPolyline>();

			IPolyline segment = GeometryFactory.CreatePolyline(0, 0, 0,
			                                                   1, 1, 1000.01);
			const bool dontSimplify = true;
			IPolyline errorSegment1 = GeometryFactory.CreatePolyline(
				0.999991911154811, 0.999991914880027, 1000,
				1.00000191107392, 1.00000191479921, 1000.01,
				dontSimplify);
			expectedErrorGeometries.Add(errorSegment1);
			TestPolycurve(segment, expectedErrorGeometries, 0, 1000);
		}

		[Test]
		public void TestSegmentsOneBelow()
		{
			var expectedErrorGeometries = new List<IPolyline>();

			IPolyline segment = GeometryFactory.CreatePolyline(2000000, 1000000, 200,
			                                                   2000200, 1001000, 75);
			IPolyline errorSegment1 = GeometryFactory.CreatePolyline(2000160, 1000800, 100,
			                                                         2000200, 1001000, 75);
			expectedErrorGeometries.Add(errorSegment1);
			TestPolycurve(segment, expectedErrorGeometries, 100, 300);
		}

		[Test]
		public void TestSegmentsBothInside()
		{
			var expectedErrorGeometries = new List<IPolyline>();

			IPolyline segment = GeometryFactory.CreatePolyline(2000000, 1000000, 200,
			                                                   2000000, 1001000, 200);
			TestPolycurve(segment, expectedErrorGeometries, 100, 300);
		}

		[Test]
		public void TestSegmentsFirstBelowSecondAbove()
		{
			var expectedErrorGeometries = new List<IPolyline>();

			IPolyline segment = GeometryFactory.CreatePolyline(2000000, 1000000, 0,
			                                                   2000000, 1001000, 400);
			IPolyline errorSegment1 = GeometryFactory.CreatePolyline(2000000, 1000000, 0,
			                                                         2000000, 1000250, 100);
			IPolyline errorSegment2 = GeometryFactory.CreatePolyline(2000000, 1000750, 300,
			                                                         2000000, 1001000, 400);
			expectedErrorGeometries.Add(errorSegment1);
			expectedErrorGeometries.Add(errorSegment2);
			TestPolycurve(segment, expectedErrorGeometries, 100, 300);
		}

		[Test]
		public void TestSegmentsFirstAboveSecondBelow()
		{
			var expectedErrorGeometries = new List<IPolyline>();

			IPolyline segment = GeometryFactory.CreatePolyline(2000000, 1000000, 400,
			                                                   2000000, 1001000, 0);
			IPolyline errorSegment1 = GeometryFactory.CreatePolyline(2000000, 1000000, 400,
			                                                         2000000, 1000250, 300);
			IPolyline errorSegment2 = GeometryFactory.CreatePolyline(2000000, 1000750, 100,
			                                                         2000000, 1001000, 0);
			expectedErrorGeometries.Add(errorSegment1);
			expectedErrorGeometries.Add(errorSegment2);
			TestPolycurve(segment, expectedErrorGeometries, 100, 300);
		}

		private static IList<IGeometry> RunTest([NotNull] IFeatureClass featureClass,
		                                        double minZValue,
		                                        double maxZValue,
		                                        int expectedErrorCount)
		{
			var test = new QaWithinZRange(featureClass, minZValue, maxZValue);

			var runner = new QaTestRunner(test) {KeepGeometry = true};
			runner.Execute();
			Assert.AreEqual(expectedErrorCount, runner.Errors.Count);

			return runner.ErrorGeometries;
		}

		private void TestPolycurve(IPolycurve errorPolycurve,
		                           ICollection<IPolyline> expectedErrorLines,
		                           double minZValue,
		                           double maxZValue)
		{
			var expectedErrorLinesCopy = new List<IPolyline>(expectedErrorLines);

			_featureClassNumber++;
			string featureClassName = string.Format("linetest_{0}", _featureClassNumber);

			IFeatureClass featureClass =
				TestWorkspaceUtils.CreateSimpleFeatureClass(
					_testWs, featureClassName, null,
					errorPolycurve.GeometryType,
					esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, 0d,
					true);

			IFeature row = featureClass.CreateFeature();
			row.Shape = errorPolycurve;
			row.Store();

			IList<IGeometry> errorGeometries = RunTest(featureClass, minZValue, maxZValue,
			                                           expectedErrorLines.Count);

			Assert.AreEqual(expectedErrorLines.Count, errorGeometries.Count);

			foreach (IGeometry errorGeometry in errorGeometries)
			{
				foreach (IPolyline expectedErrorGeometry in expectedErrorLinesCopy)
				{
					if (GeometryUtils.AreEqual(errorGeometry, expectedErrorGeometry))
					{
						expectedErrorLinesCopy.Remove(expectedErrorGeometry);
						break;
					}
				}
			}

			if (expectedErrorLinesCopy.Count != 0)
			{
				Console.WriteLine(@"Expected error lines not reported:");
				foreach (IPolyline polyline in expectedErrorLinesCopy)
				{
					Console.WriteLine(@"Expected error line:");
					Console.WriteLine(GeometryUtils.ToString(polyline));
				}

				Console.WriteLine(@"Reported error lines:");
				foreach (IGeometry errorGeometry in errorGeometries)
				{
					Console.WriteLine(@"Reported error line:");
					Console.WriteLine(GeometryUtils.ToString(errorGeometry));
				}
			}

			Assert.AreEqual(0, expectedErrorLinesCopy.Count);
		}
	}
}

using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.QA.Tests.ParameterTypes;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaZDifferenceSelfTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanTestPolygonMultiPatches()
		{
			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			var multiPatchClass = new FeatureClassMock(
				"multipatch",
				esriGeometryType.esriGeometryMultiPatch,
				1, esriFeatureType.esriFTSimple, sref);
			var polygonClass = new FeatureClassMock(
				"polygon", esriGeometryType.esriGeometryPolygon,
				1, esriFeatureType.esriFTSimple, sref);

			var multiPatch = new MultiPatchConstruction();

			multiPatch.StartOuterRing(0, 0, 0)
			          .Add(5, 0, 0)
			          .Add(5, 1, 5)
			          .Add(0, 1, 5);

			IFeature multiPatchRow = multiPatchClass.CreateFeature(multiPatch.MultiPatch);

			CurveConstruction polygon =
				CurveConstruction.StartPoly(0, 0, 10)
				                 .LineTo(10, 0, 10)
				                 .LineTo(0, 10, 10);

			IFeature polygonRow = polygonClass.CreateFeature(polygon.ClosePolygon());

			var test = new QaZDifferenceSelfWrapper(
				new[]
				{
					ReadOnlyTableFactory.Create(multiPatchClass),
					ReadOnlyTableFactory.Create(polygonClass)
				},
				20, 0,
				ZComparisonMethod.BoundingBox,
				null);
			var runner = new QaTestRunner(test);
			int errorCount = test.TestDirect(ReadOnlyRow.Create(multiPatchRow), 0,
			                                 ReadOnlyRow.Create(polygonRow), 1);
			Assert.AreEqual(1, errorCount);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void CanTestNaNZs()
		{
			var multiPatchClass = new FeatureClassMock(
				"multipatch", esriGeometryType.esriGeometryMultiPatch, 1);
			var polygonClass = new FeatureClassMock(
				"polygon", esriGeometryType.esriGeometryPolygon, 1);

			var multiPatch = new MultiPatchConstruction();

			multiPatch.StartOuterRing(0, 0, 0)
			          .Add(5, 0, 0)
			          .Add(5, 1, 5)
			          .Add(0, 1, 5);

			IFeature multiPatchRow = multiPatchClass.CreateFeature(multiPatch.MultiPatch);

			CurveConstruction polygon =
				CurveConstruction.StartPoly(0, 0)
				                 .LineTo(10, 0)
				                 .LineTo(0, 10);

			IFeature polygonRow = polygonClass.CreateFeature(polygon.ClosePolygon());

			var test = new QaZDifferenceSelfWrapper(
				new[]
				{
					ReadOnlyTableFactory.Create(multiPatchClass),
					ReadOnlyTableFactory.Create(polygonClass)
				},
				20, 0,
				ZComparisonMethod.BoundingBox,
				null);

			var runner = new QaTestRunner(test);
			int errorCount = test.TestDirect(
				ReadOnlyRow.Create(multiPatchRow), 0, ReadOnlyRow.Create(polygonRow), 1);
			Assert.AreEqual(1, errorCount);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void CanTestLines()
		{
			var lineClass = new FeatureClassMock("line", esriGeometryType.esriGeometryPolyline, 1);

			CurveConstruction line1 =
				CurveConstruction.StartLine(0, 0, 10)
				                 .LineTo(10, 0, 10)
				                 .LineTo(0, 10, 10);

			IFeature row1 = lineClass.CreateFeature(line1.Curve);

			CurveConstruction line2 =
				CurveConstruction.StartLine(2, -2, 8)
				                 .LineTo(2, 20, 8);

			IFeature row2 = lineClass.CreateFeature(line2.Curve);

			var test = new QaZDifferenceSelfWrapper(
				new[] { ReadOnlyTableFactory.Create(lineClass) },
				3, 0,
				ZComparisonMethod.BoundingBox,
				null);
			var runner = new QaTestRunner(test);
			int errorCount = test.TestDirect(
				ReadOnlyRow.Create(row1), 0, ReadOnlyRow.Create(row2), 0);
			Assert.AreEqual(1, errorCount);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void CanTestLineIntersections()
		{
			var lineClass = new FeatureClassMock("line", esriGeometryType.esriGeometryPolyline, 1);

			CurveConstruction line1 =
				CurveConstruction.StartLine(0, 0, 10)
				                 .LineTo(10, 0, 10)
				                 .LineTo(0, 10, 10);

			IFeature row1 = lineClass.CreateFeature(line1.Curve);

			CurveConstruction line2 =
				CurveConstruction.StartLine(2, -2, 8)
				                 .LineTo(2, 20, 8);

			IFeature row2 = lineClass.CreateFeature(line2.Curve);

			var test = new QaZDifferenceSelfWrapper(
				new[] { ReadOnlyTableFactory.Create(lineClass) },
				3, 0,
				ZComparisonMethod.IntersectionPoints,
				null);
			var runner = new QaTestRunner(test);
			int errorCount =
				test.TestDirect(ReadOnlyRow.Create(row1), 0, ReadOnlyRow.Create(row2), 0);
			Assert.AreEqual(2, errorCount);
			Assert.AreEqual(2, runner.Errors.Count);
		}

		[Test]
		public void CanTestLinesMaximumDifference()
		{
			var lineClass = new FeatureClassMock("line", esriGeometryType.esriGeometryPolyline, 1);

			CurveConstruction line1 =
				CurveConstruction.StartLine(0, 0, 10)
				                 .LineTo(10, 0, 10)
				                 .LineTo(0, 10, 10);

			IFeature row1 = lineClass.CreateFeature(line1.Curve);

			CurveConstruction line2 =
				CurveConstruction.StartLine(2, -2, 21)
				                 .LineTo(2, 20, 21);

			IFeature row2 = lineClass.CreateFeature(line2.Curve);

			var test = new QaZDifferenceSelfWrapper(
				new[] { ReadOnlyTableFactory.Create(lineClass) },
				0, 10,
				ZComparisonMethod.BoundingBox,
				null);
			var runner = new QaTestRunner(test);
			int errorCount =
				test.TestDirect(ReadOnlyRow.Create(row1), 0, ReadOnlyRow.Create(row2), 0);
			Assert.AreEqual(1, errorCount);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void CanTestConstraint()
		{
			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			var multiPatchClass = new FeatureClassMock("multipatch", esriGeometryType.esriGeometryMultiPatch,
			                                           null, esriFeatureType.esriFTSimple, sref);
			multiPatchClass.AddField("Level", esriFieldType.esriFieldTypeInteger);
			int levelIndex = multiPatchClass.FindField("Level");

			var polygonClass = new FeatureClassMock("polygon", esriGeometryType.esriGeometryPolygon,
			                                        null, esriFeatureType.esriFTSimple, sref);
			polygonClass.AddField("Level", esriFieldType.esriFieldTypeInteger);

			var multiPatchConstruction = new MultiPatchConstruction();

			multiPatchConstruction.StartOuterRing(0, 0, 0)
			                      .Add(5, 0, 0)
			                      .Add(5, 1, 5)
			                      .Add(0, 1, 5);

			IFeature multiPatchRow =
				multiPatchClass.CreateFeature(multiPatchConstruction.MultiPatch);

			multiPatchRow.set_Value(levelIndex, 2);

			CurveConstruction curveConstruction =
				CurveConstruction.StartPoly(0, 0, 50)
				                 .LineTo(10, 0, 50)
				                 .LineTo(0, 10, 50);

			IFeature polygonRow =
				polygonClass.CreateFeature(curveConstruction.ClosePolygon());
			polygonRow.set_Value(levelIndex, 1);

			var test = new QaZDifferenceSelfWrapper(
				new[]
				{
					ReadOnlyTableFactory.Create(multiPatchClass),
					ReadOnlyTableFactory.Create(polygonClass)
				},
				20, 0,
				ZComparisonMethod.BoundingBox,
				"U.Level > L.Level");

			var runner = new QaTestRunner(test);
			int errorCount = test.TestDirect(
				ReadOnlyRow.Create(multiPatchRow), 0, ReadOnlyRow.Create(polygonRow), 1);
			Assert.AreEqual(1, errorCount);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		private class QaZDifferenceSelfWrapper : QaZDifferenceSelf
		{
			public QaZDifferenceSelfWrapper(
				IList<IReadOnlyFeatureClass> featureClasses,
				double minimumZDifference,
				double maximumZDifference,
				ZComparisonMethod zComparisonMethod, string zRelationConstraint)
				: base(featureClasses, minimumZDifference, maximumZDifference,
				       zComparisonMethod, zRelationConstraint) { }

			/// <summary>
			/// Assumption calling this method: row0 and row1 do intersect
			/// </summary>
			public int TestDirect(IReadOnlyRow row0, int tableIndex0, IReadOnlyRow row1,
			                      int tableIndex1)
			{
				return FindErrors(row0, tableIndex0, row1, tableIndex1);
			}
		}
	}
}

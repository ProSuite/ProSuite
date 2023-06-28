using System;
using System.Collections.Generic;
using System.Diagnostics;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Testing;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaDuplicateGeometrySelfTest
	{
		private IFeatureWorkspace _testWs;
		private const double _xyTolerance = 0.001;
		private const double _xyResolution = 0.0001;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();

			_testWs = TestWorkspaceUtils.CreateTestFgdbWorkspace("QaDuplicateGeometrySelfTest");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void LearningTestEqualityVertexOnLine()
		{
			ISpatialReference sref = CreateSpatialReference();
			IPolycurve shape2D1 = CurveConstruction
			                      .StartLine(100, 0)
			                      .LineTo(200, 0)
			                      .LineTo(300, 0)
			                      .Curve;

			IPolycurve shape2D2 = CurveConstruction
			                      .StartLine(100, 0)
			                      .LineTo(300, 0)
			                      .Curve;

			shape2D1.SpatialReference = sref;
			shape2D2.SpatialReference = sref;

			Assert.IsTrue(((IRelationalOperator) shape2D1).Equals(shape2D2),
			              "IRelationalOperator.Equals");
			Assert.IsFalse(((IClone) shape2D1).IsEqual((IClone) shape2D2), "IClone.IsEqual");
		}

		[Test]
		public void LearningTestEqualityZDifference()
		{
			ISpatialReference sref = CreateSpatialReference();

			IPolycurve shape2D1 = CurveConstruction
			                      .StartLine(100, 0, 1000)
			                      .LineTo(200, 0, 1000)
			                      .LineTo(300, 0, 1000)
			                      .Curve;

			IPolycurve shape2D2 = CurveConstruction
			                      .StartLine(100, 0, 1000)
			                      .LineTo(200, 0, 2000) // z is different
			                      .LineTo(300, 0, 1000)
			                      .Curve;

			shape2D1.SpatialReference = sref;
			shape2D2.SpatialReference = sref;

			Assert.IsTrue(((IRelationalOperator) shape2D1).Equals(shape2D2),
			              "IRelationalOperator.Equals");
			Assert.IsFalse(((IClone) shape2D1).IsEqual((IClone) shape2D2), "IClone.IsEqual");
		}

		[Test]
		public void LearningTestEqualityTolerance()
		{
			ISpatialReference sref = CreateSpatialReference();
			IPolycurve shape2D1 = CurveConstruction
			                      .StartLine(100, 0)
			                      .LineTo(200, 0)
			                      .LineTo(300, 0)
			                      .Curve;

			IPolycurve shape2D2 = CurveConstruction
			                      .StartLine(100, 0)
			                      .LineTo(200 + 2 * _xyResolution, 0)
			                      .LineTo(300, 0)
			                      .Curve;

			shape2D1.SpatialReference = sref;
			shape2D2.SpatialReference = sref;

			// IClone.IsEqual also applies xy tolerance, per vertex

			Assert.IsTrue(((IRelationalOperator) shape2D1).Equals(shape2D2),
			              "IRelationalOperator.Equals");
			Assert.IsTrue(((IClone) shape2D1).IsEqual((IClone) shape2D2), "IClone.IsEqual");
		}

		[Test]
		public void TestReportSingleErrorPerDuplicateSet()
		{
			IFeatureClass featureClass = CreateLineFeatureClass(
				_testWs, "TestReportSingleErrorPerDuplicateSet");

			// make sure the table is known by the workspace
			((IWorkspaceEdit) _testWs).StartEditing(false);
			((IWorkspaceEdit) _testWs).StopEditing(true);

			IPolycurve shape1 = CurveConstruction.StartLine(100, 0)
			                                     .LineTo(200, 0)
			                                     .Curve;

			IPolycurve shape2 = CurveConstruction.StartLine(200, 100)
			                                     .LineTo(300, 100)
			                                     .Curve;

			IPolycurve shape3 = CurveConstruction.StartLine(300, 200)
			                                     .LineTo(400, 200)
			                                     .Curve;

			IPolycurve shape4 = CurveConstruction.StartLine(300, 300)
			                                     .LineTo(400, 300)
			                                     .Curve;

			AddFeature(featureClass, shape1);
			AddFeature(featureClass, shape2);
			AddFeature(featureClass, shape3);

			AddFeature(featureClass, shape4);

			// 1. set of duplicates
			AddFeature(featureClass, shape1);
			AddFeature(featureClass, shape2);
			AddFeature(featureClass, shape3);

			// 2. set of duplicates
			AddFeature(featureClass, shape1);
			AddFeature(featureClass, shape2);
			AddFeature(featureClass, shape3);

			// add more duplicates, per geometry

			AddFeature(featureClass, shape1);
			AddFeature(featureClass, shape1);
			AddFeature(featureClass, shape1);

			AddFeature(featureClass, shape2);
			AddFeature(featureClass, shape2);
			AddFeature(featureClass, shape2);

			AddFeature(featureClass, shape3);
			AddFeature(featureClass, shape3);
			AddFeature(featureClass, shape3);

			var test =
				new QaDuplicateGeometrySelf(ReadOnlyTableFactory.Create(featureClass), null, true);

			var testRunner = new QaContainerTestRunner(50, test);
			testRunner.Execute();
			IList<QaError> errors = testRunner.Errors;
			Assert.AreEqual(3, errors.Count);
			Assert.AreEqual(6, errors[0].InvolvedRows.Count);
			Assert.AreEqual(6, errors[1].InvolvedRows.Count);
			Assert.AreEqual(6, errors[2].InvolvedRows.Count);
		}

		[Test]
		public void TestReportDuplicatesPairwise()
		{
			IFeatureClass featureClass = CreateLineFeatureClass(_testWs, "TestCombinedLists");

			// make sure the table is known by the workspace
			((IWorkspaceEdit) _testWs).StartEditing(false);
			((IWorkspaceEdit) _testWs).StopEditing(true);

			IPolycurve shape1 = CurveConstruction.StartLine(100, 0)
			                                     .LineTo(200, 0)
			                                     .Curve;

			IPolycurve shape2 = CurveConstruction.StartLine(200, 100)
			                                     .LineTo(300, 100)
			                                     .Curve;

			IPolycurve shape3 = CurveConstruction.StartLine(300, 200)
			                                     .LineTo(400, 200)
			                                     .Curve;

			AddFeature(featureClass, shape1);
			AddFeature(featureClass, shape2);
			AddFeature(featureClass, shape3);

			// first set of duplicates
			AddFeature(featureClass, shape1);
			AddFeature(featureClass, shape2);
			AddFeature(featureClass, shape3);

			// second set of duplicates
			AddFeature(featureClass, shape1);
			AddFeature(featureClass, shape2);
			AddFeature(featureClass, shape3);

			var test = new QaDuplicateGeometrySelf(
				ReadOnlyTableFactory.Create(featureClass), null, false);

			var testRunner = new QaContainerTestRunner(1000, test);
			testRunner.Execute();

			Assert.AreEqual(9, testRunner.Errors.Count);
		}

		[Test]
		public void TestReportDuplicatesMultipatchPerformance()
		{
			string path = TestDataPreparer.ExtractZip("GebZueriberg.gdb.zip").Overwrite().GetPath();

			IFeatureWorkspace featureWorkspace = WorkspaceUtils.OpenFileGdbFeatureWorkspace(path);
			IFeatureClass featureClass =
				DatasetUtils.OpenFeatureClass(featureWorkspace, "TLM_GEBAEUDE");

			var test = new QaDuplicateGeometrySelf(
				ReadOnlyTableFactory.Create(featureClass), null, false);

			var testRunner = new QaContainerTestRunner(1000, test);

			Stopwatch watch = Stopwatch.StartNew();
			testRunner.Execute();
			watch.Stop();

			Assert.AreEqual(0, testRunner.Errors.Count);

			// TODO: Add assertion, that it is < 1 second...
			Console.WriteLine($"Processed real data in {watch.ElapsedMilliseconds}ms");
		}

		private static void AddFeature([NotNull] IFeatureClass featureClass,
		                               [NotNull] IGeometry shape)
		{
			IFeature feature = featureClass.CreateFeature();

			feature.Shape = GeometryFactory.Clone(shape);

			feature.Store();
		}

		[NotNull]
		private static IFeatureClass CreateLineFeatureClass(
			[NotNull] IFeatureWorkspace workspace, [NotNull] string name)
		{
			ISpatialReference spatialReference = CreateSpatialReference();

			return DatasetUtils.CreateSimpleFeatureClass(
				workspace, name,
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						"SHAPE", esriGeometryType.esriGeometryPolyline,
						spatialReference, 1000, false, false)),
				null);
		}

		[NotNull]
		private static ISpatialReference CreateSpatialReference()
		{
			ISpatialReference result = SpatialReferenceUtils.CreateSpatialReference
			((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
			 true);
			SpatialReferenceUtils.SetXYDomain(result, -1000, -1000, 1000, 1000,
			                                  _xyResolution, _xyTolerance);
			return result;
		}
	}
}

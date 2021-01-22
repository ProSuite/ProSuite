using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaDuplicateGeometrySelfTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _testWs;
		private const double _xyTolerance = 0.001;
		private const double _xyResolution = 0.0001;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);

			_testWs = TestWorkspaceUtils.CreateTestFgdbWorkspace("QaDuplicateGeometrySelfTest");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
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

			var test = new QaDuplicateGeometrySelf(featureClass, null, true);

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

			var test = new QaDuplicateGeometrySelf(featureClass, null, false);

			var testRunner = new QaContainerTestRunner(1000, test);
			testRunner.Execute();

			Assert.AreEqual(9, testRunner.Errors.Count);
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

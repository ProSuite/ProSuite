using System;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaNonEmptyGeometryTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense(EnvironmentUtils.Is64BitProcess, activateAdvancedLicense:true);
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanTestNonEmptyGeometry_Polyline()
		{
			IFeatureClass fc = new FeatureClassMock("LineFc",
			                                        esriGeometryType.esriGeometryPolyline, 1);

			IFeature feature = fc.CreateFeature();

			feature.Shape = CurveConstruction.StartLine(0, 0).LineTo(10, 10).Curve;
			feature.Store();

			var test = new QaNonEmptyGeometry(ReadOnlyTableFactory.Create(fc));
			var runner = new QaTestRunner(test);
			runner.Execute(feature);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanTestNonEmptyGeometry_Point()
		{
			IFeatureClass fc = new FeatureClassMock("PointFc",
			                                        esriGeometryType.esriGeometryPoint, 1);

			IFeature feature = fc.CreateFeature();

			feature.Shape = GeometryFactory.CreatePoint(0, 0);
			feature.Store();

			var test = new QaNonEmptyGeometry(ReadOnlyTableFactory.Create(fc));
			var runner = new QaTestRunner(test);
			runner.Execute(feature);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanTestNullGeometry()
		{
			IFeatureClass fc = new FeatureClassMock("LineFc",
			                                        esriGeometryType.esriGeometryPolyline, 1);

			IFeature feature = fc.CreateFeature();

			feature.Shape = null;
			feature.Store();

			var test = new QaNonEmptyGeometry(ReadOnlyTableFactory.Create(fc));
			var runner = new QaTestRunner(test);
			runner.Execute(feature);

			AssertUtils.OneError(runner, "EmptyGeometry.GeometryNull");
		}

		[Test]
		public void CanTestEmptyGeometry()
		{
			IFeatureClass fc = new FeatureClassMock("LineFc",
			                                        esriGeometryType.esriGeometryPolyline, 1);

			IFeature feature = fc.CreateFeature();

			feature.Shape = new PolylineClass();
			feature.Store();

			var test = new QaNonEmptyGeometry(ReadOnlyTableFactory.Create(fc));
			var runner = new QaTestRunner(test);
			runner.Execute(feature);

			AssertUtils.OneError(runner, "EmptyGeometry.GeometryEmpty");
		}

		[Test]
		public void CanTestNullGeometry_DontFilterPolycurvesByZeroLength()
		{
			IFeatureClass fc = new FeatureClassMock("LineFc",
			                                        esriGeometryType.esriGeometryPolyline, 1);

			IFeature feature = fc.CreateFeature();

			feature.Shape = null;
			feature.Store();

			const bool dontFilterPolycurvesByZeroLength = true;
			var test = new QaNonEmptyGeometry(ReadOnlyTableFactory.Create(fc),
			                                  dontFilterPolycurvesByZeroLength);

			var runner = new QaTestRunner(test);
			runner.Execute(feature);

			AssertUtils.OneError(runner, "EmptyGeometry.GeometryNull");
		}

		[Test]
		public void CanTestEmptyGeometryFgdbPolyline()
		{
			IFeatureWorkspace featureWorkspace =
				TestWorkspaceUtils.CreateTestFgdbWorkspace("EmptyPolyline");

			IFeature feature =
				CreateEmptyGeometryFeature(featureWorkspace, esriGeometryType.esriGeometryPolyline);

			AssertOneErrorEmptyGeometry((IFeatureClass) feature.Class);
		}

		[Test]
		public void CanTestEmptyGeometryFgdbMultipatch()
		{
			IFeatureWorkspace featureWorkspace =
				TestWorkspaceUtils.CreateTestFgdbWorkspace("EmptyMultipatch");

			IFeature feature =
				CreateEmptyGeometryFeature(featureWorkspace,
				                           esriGeometryType.esriGeometryMultiPatch);

			AssertOneErrorEmptyGeometry((IFeatureClass) feature.Class);
		}

		[Test]
		[Category(TestCategory.Sde)]
		[Category(Commons.Test.TestCategory.FixMe)]
		public void CanTestEmptyGeometrySdeMultipatch()
		{
			IWorkspace workspace = TestUtils.OpenUserWorkspaceOracle();
			//IWorkspace workspace = ProSuite.Commons.AO.Test.TestUtils.OpenSDEWorkspaceOracle();

			IMultiPatch normalGeometry = GeometryFactory.CreateMultiPatch(
				GeometryFactory.CreateRing(new[]
				                           {
					                           new WKSPointZ { X = 2600000, Y = 1200000, Z = 400 },
					                           new WKSPointZ { X = 2600000, Y = 1200100, Z = 400 },
					                           new WKSPointZ { X = 2600100, Y = 1200000, Z = 400 },
					                           new WKSPointZ { X = 2600000, Y = 1200000, Z = 400 },
				                           }));

			GeometryUtils.MakeZAware(normalGeometry);

			IFeature feature =
				CreateEmptyGeometryFeature((IFeatureWorkspace) workspace,
				                           esriGeometryType.esriGeometryMultiPatch, normalGeometry);

			AssertOneErrorEmptyGeometry((IFeatureClass) feature.Class);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanTestEmptyGeometrySdePolyline()
		{
			IWorkspace workspace = TestUtils.OpenUserWorkspaceOracle();
			//IWorkspace workspace = ProSuite.Commons.AO.Test.TestUtils.OpenSDEWorkspaceOracle();

			IPolyline normalGeometry = GeometryFactory.CreatePolyline(
				GeometryFactory.CreatePath(new PointClass { X = 2600000, Y = 1200000, Z = 400 },
				                           new PointClass { X = 2600000, Y = 1200100, Z = 400 },
				                           new PointClass { X = 2600100, Y = 1200000, Z = 400 },
				                           new PointClass { X = 2600000, Y = 1200000, Z = 400 }));

			GeometryUtils.MakeZAware(normalGeometry);

			IFeature feature =
				CreateEmptyGeometryFeature((IFeatureWorkspace) workspace,
				                           esriGeometryType.esriGeometryPolyline, normalGeometry);

			AssertOneErrorEmptyGeometry((IFeatureClass) feature.Class);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanTestEmptyGeometrySdePoint()
		{
			IWorkspace workspace = TestUtils.OpenUserWorkspaceOracle();
			//IWorkspace workspace = ProSuite.Commons.AO.Test.TestUtils.OpenSDEWorkspaceOracle();

			IPoint normalGeometry = GeometryFactory.CreatePoint(2600000, 1200000, 400);

			GeometryUtils.MakeZAware(normalGeometry);

			IFeature feature =
				CreateEmptyGeometryFeature((IFeatureWorkspace) workspace,
				                           esriGeometryType.esriGeometryPoint, normalGeometry);

			AssertOneErrorEmptyGeometry((IFeatureClass) feature.Class);
		}

		private static void AssertOneErrorEmptyGeometry(IFeatureClass featureClass)
		{
			var test = new QaNonEmptyGeometry(ReadOnlyTableFactory.Create(featureClass));
			var runner = new QaTestRunner(test);
			runner.Execute();

			AssertUtils.OneError(runner, "EmptyGeometry.GeometryEmpty");
		}

		private IFeature CreateEmptyGeometryFeature(IFeatureWorkspace ws,
		                                            esriGeometryType geometryType,
		                                            IGeometry andAlso = null)
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference
			((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
			 true);

			IFeatureClass featureClass =
				EnsureFeatureClass(ws, geometryType, geometryType.ToString(), sr);

			IGdbTransaction transaction = new GdbTransaction();

			IFeature feature = null;
			bool inserted = transaction.Execute((IWorkspace) ws, () =>
			{
				if (andAlso != null)
				{
					andAlso.SpatialReference = sr;
					IFeature nonEmpty = featureClass.CreateFeature();
					nonEmpty.Shape = andAlso;
					nonEmpty.Store();
				}

				feature = featureClass.CreateFeature();

				// NOTE: There is no difference when setting an empty geometry
				feature.Store();
			}, "Insert feature");

			Assert.IsTrue(inserted);

			return feature;
		}

		private static IFeatureClass EnsureFeatureClass(IFeatureWorkspace ws,
		                                                esriGeometryType geometryType,
		                                                string name,
		                                                ISpatialReference sr)
		{
			try
			{
				IFeatureClass existing = DatasetUtils.OpenFeatureClass(ws, name);
				DatasetUtils.DeleteFeatureClass(existing);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());

			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", geometryType,
				                sr, 1000, true));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, name, fields);

			if (fc is IVersionedTable)
			{
				DatasetUtils.RegisterAsVersioned(fc);
			}

			return fc;
		}
	}
}

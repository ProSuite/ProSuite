using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Container.TestContainer;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestData;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaCentroidsTest
	{
		private const double _xyTolerance = 0.001;
		private int _errorCount;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void MultipartTest()
		{
			IFeatureWorkspace workspace =
				TestWorkspaceUtils.CreateInMemoryWorkspace("MultipartTest");

			IFeatureClass linesFc = CreateFeatureClass(workspace, "Border",
			                                           esriGeometryType.esriGeometryPolyline);
			IFeatureClass pointsFc = CreateFeatureClass(workspace, "Centr",
			                                            esriGeometryType.esriGeometryPoint);

			AddFeature(
				linesFc,
				CurveConstruction.StartLine(0, 0).LineTo(4, 0).MoveTo(6, 0).LineTo(6, 10).Curve);
			AddFeature(linesFc, CurveConstruction.StartLine(4, 10).LineTo(6, 10).Curve);
			AddFeature(
				linesFc,
				CurveConstruction.StartLine(4, 0).LineTo(6, 0).MoveTo(4, 10).LineTo(0, 0).Curve);

			// expect counter-clockwise: 0 errors
			var runner = new QaContainerTestRunner(
				1000, new QaCentroids(ReadOnlyTableFactory.Create(linesFc),
				                      ReadOnlyTableFactory.Create(pointsFc)));
			Assert.AreEqual(1, runner.Execute());
		}

		[Test]
		[Ignore("TODO where to get the required test data?")]
		public void TestV200Data()
		{
			var ws = (IFeatureWorkspace)
				TestDataUtils.OpenFileGdb("V200_admin.gdb");
			IFeatureClass border = ws.OpenFeatureClass("VEC200_COM_BOUNDARY");
			IFeatureClass center = ws.OpenFeatureClass("VEC200_COM_ZENTROID");
			var test = new QaCentroids(ReadOnlyTableFactory.Create(border),
			                           ReadOnlyTableFactory.Create(center));

			var container = new TestContainer();
			container.AddTest(test);
			container.QaError += container_QaError;

			_errorCount = 0;
			container.Execute();

			Assert.IsTrue(_errorCount == 0);
		}

		private static IFeatureClass CreateFeatureClass(IFeatureWorkspace workspace,
		                                                string name,
		                                                esriGeometryType type)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", type, CreateLV95(), 1000));

			return DatasetUtils.CreateSimpleFeatureClass(workspace, name, fields);
		}

		[NotNull]
		private static IFeature AddFeature(
			[NotNull] IFeatureClass featureClass,
			[NotNull] IGeometry geometry)
		{
			IFeature feature = featureClass.CreateFeature();
			feature.Shape = geometry;

			feature.Store();

			return feature;
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

		private void container_QaError(object sender, QaErrorEventArgs e)
		{
			_errorCount++;
		}
	}
}

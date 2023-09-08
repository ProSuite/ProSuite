using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
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
			runner.Execute();
			AssertUtils.OneError(runner, "Centroids.NoCentroid", 3);
		}

		[Test]
		public void MultipartWithSeveralCentroidsTest()
		{
			IFeatureWorkspace workspace =
				TestWorkspaceUtils.CreateInMemoryWorkspace("MultipartWithSeveralCentroids");

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

			AddFeature(pointsFc, GeometryFactory.CreatePoint(5, 2));
			AddFeature(pointsFc, GeometryFactory.CreatePoint(6, 3));
			AddFeature(pointsFc, GeometryFactory.CreatePoint(5, 4));

			// expect counter-clockwise: 0 errors
			var runner = new QaContainerTestRunner(
				1000, new QaCentroids(ReadOnlyTableFactory.Create(linesFc),
									  ReadOnlyTableFactory.Create(pointsFc)));
			Assert.AreEqual(2, runner.Execute());
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

		[Test]
		public void CanUseConstraint()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateTestFgdbWorkspace("QaCentroid");
			ISpatialReference sr = CreateLV95();
			IFeatureClass fcLine = DatasetUtils.CreateSimpleFeatureClass(ws, "line",
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateIntegerField("intLine"),
					FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPolyline, sr)
				));

			IFeatureClass fcPnt = DatasetUtils.CreateSimpleFeatureClass(ws, "pnt",
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateIntegerField("intPnt"),
					FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPoint, sr)
				));

			{
				IFeature f = fcLine.CreateFeature();
				f.Shape = CurveConstruction.StartLine(10, 10).LineTo(15, 20).LineTo(20, 10)
										   .Curve;
				f.Value[1] = 5;
				f.Store();
			}
			{
				IFeature f = fcLine.CreateFeature();
				f.Shape = CurveConstruction.StartLine(10, 10).LineTo(20, 10).Curve;
				f.Value[1] = 5;
				f.Store();
			}
			{
				IFeature f = fcLine.CreateFeature();
				f.Shape = CurveConstruction.StartLine(10, 10).LineTo(15, 1).LineTo(20, 10)
										   .Curve;
				f.Value[1] = 5;
				f.Store();
			}

			{
				IFeature f = fcPnt.CreateFeature();
				f.Shape = GeometryFactory.CreatePoint(15, 12);
				f.Value[1] = 5;
				f.Store();
			}
			{
				IFeature f = fcPnt.CreateFeature();
				f.Shape = GeometryFactory.CreatePoint(15, 8);
				f.Value[1] = 5;
				f.Store();
			}

			IReadOnlyFeatureClass roLine = ReadOnlyTableFactory.Create(fcLine);
			IReadOnlyFeatureClass roPnt = ReadOnlyTableFactory.Create(fcPnt);


			{
				QaCentroids test = new QaCentroids(roLine, roPnt, "B.intLine = 5");
				int ne = 0;
				test.QaError += (s, a) => { Assert.NotNull(a); ne++; };
				int n = test.Execute();
				Assert.AreEqual(0, n + ne);
			}
			{
				QaCentroids test = new QaCentroids(roLine, roPnt, "B.intLine = 5");
				var containerRunner = new QaContainerTestRunner(100, test);
				containerRunner.Execute();
				Assert.AreEqual(0, containerRunner.Errors.Count);
			}
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
				((int)esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);
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

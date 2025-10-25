using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Tests.ParameterTypes;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaOrphanNodeTest
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
		public void CanTestOrphanNode()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("CanTestOrphanNode");

			IFeatureClass fcPoints = CreatePointClass(ws);
			IFeatureClass fcLines = CreateLineClass(ws);

			IFeature point1 = fcPoints.CreateFeature();
			point1.Shape = GeometryFactory.CreatePoint(100, 100);
			point1.Store();

			IFeature point2 = fcPoints.CreateFeature();
			point2.Shape = GeometryFactory.CreatePoint(200, 100);
			point2.Store();

			IFeature point3 = fcPoints.CreateFeature(); //orphan point
			point3.Shape = GeometryFactory.CreatePoint(150, 150);
			point3.Store();

			IFeature line1 = fcLines.CreateFeature();
			line1.Shape = CurveConstruction.StartLine(40, 40).LineTo(60, 60).LineTo(100, 100).Curve;
			line1.Store();

			IFeature line2 = fcLines.CreateFeature();
			line2.Shape = CurveConstruction.StartLine(100, 100).LineTo(200, 100).Curve;
			line2.Store();

			var test = new QaOrphanNode(
				ReadOnlyTableFactory.Create(fcPoints),
				ReadOnlyTableFactory.Create(fcLines),
				OrphanErrorType.OrphanedPoint);

			var runner = new QaContainerTestRunner(1000, test);
			runner.Execute();

			AssertUtils.OneError(runner, "OrphanNode.OrphanNode");
		}

		[Test]
		public void CanTestMissingNode()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("CanTestMissingNode");

			IFeatureClass fcPoints = CreatePointClass(ws);
			IFeatureClass fcLines = CreateLineClass(ws);

			IFeature point1 = fcPoints.CreateFeature();
			point1.Shape = GeometryFactory.CreatePoint(100, 100);
			point1.Store();

			IFeature point2 = fcPoints.CreateFeature();
			point2.Shape = GeometryFactory.CreatePoint(200, 100);
			point2.Store();

			IFeature point3 = fcPoints.CreateFeature();
			point3.Shape = GeometryFactory.CreatePoint(150, 150);
			point3.Store();

			IFeature line1 = fcLines.CreateFeature(); //line start without point
			line1.Shape = CurveConstruction.StartLine(40, 40).LineTo(60, 60).LineTo(100, 100).Curve;
			line1.Store();

			IFeature line2 = fcLines.CreateFeature();
			line2.Shape = CurveConstruction.StartLine(100, 100).LineTo(200, 100).Curve;
			line2.Store();

			var test = new QaOrphanNode(
				ReadOnlyTableFactory.Create(fcPoints),
				ReadOnlyTableFactory.Create(fcLines),
				OrphanErrorType.EndPointWithoutPoint);

			var runner = new QaContainerTestRunner(1000, test);
			runner.Execute();

			AssertUtils.OneError(runner, "OrphanNode.MissingNode");
		}

		[NotNull]
		private static IFeatureClass CreatePointClass([NotNull] IFeatureWorkspace ws)
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference
				((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);

			IFieldsEdit fieldsPoint = new FieldsClass();
			fieldsPoint.AddField(FieldUtils.CreateOIDField());
			fieldsPoint.AddField(FieldUtils.CreateShapeField(
				                     "Shape", esriGeometryType.esriGeometryPoint, sr, 1000));

			IFeatureClass featureClass =
				DatasetUtils.CreateSimpleFeatureClass(ws, "OrphanNodeTestPoints", fieldsPoint);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			return featureClass;
		}

		[NotNull]
		private static IFeatureClass CreateLineClass([NotNull] IFeatureWorkspace ws)
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference
				((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);

			IFieldsEdit fieldsLine = new FieldsClass();
			fieldsLine.AddField(FieldUtils.CreateOIDField());
			fieldsLine.AddField(FieldUtils.CreateShapeField(
				                    "Shape", esriGeometryType.esriGeometryPolyline, sr, 1000));

			IFeatureClass featureClass =
				DatasetUtils.CreateSimpleFeatureClass(ws, "OrphanNodeTestLines", fieldsLine);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			return featureClass;
		}
	}
}

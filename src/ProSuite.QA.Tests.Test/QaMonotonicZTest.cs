using System;
using System.Threading;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaMonotonicZTest
	{
		private const string _flipFieldName = "Flip";

		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _testWs;

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

		[NotNull]
		private IFeatureWorkspace TestWorkspace
		{
			get
			{
				return _testWs ??
				       (_testWs = TestWorkspaceUtils.CreateInMemoryWorkspace("QaMonotonicZTest"));
			}
		}

		[Test]
		public void CanFindLevelled()
		{
			IFeatureClass featureClass = CreateLineClass(TestWorkspace);

			IPolycurve line1 = CurveConstruction.StartLine(0, 0, 5)
			                                    .LineTo(1, 0, 5)
			                                    .Curve;
			IFeature row1 = featureClass.CreateFeature();
			row1.Shape = line1;
			row1.Store();

			var test = new QaMonotonicZ(ReadOnlyTableFactory.Create(featureClass))
			           {
				           AllowConstantValues = false,
				           ExpectedMonotonicity = MonotonicityDirection.Decreasing
			           };

			var runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void CanIgnoreLevelled()
		{
			IFeatureClass featureClass = CreateLineClass(TestWorkspace);

			IPolycurve line1 =
				CurveConstruction.StartLine(0, 0, 5)
				                 .LineTo(1, 0, 6)
				                 .LineTo(2, 0, 6)
				                 .LineTo(3, 0, 7)
				                 .Curve;
			IFeature row1 = featureClass.CreateFeature();
			row1.Shape = line1;
			row1.Store();

			var test = new QaMonotonicZ(ReadOnlyTableFactory.Create(featureClass))
			           {
				           AllowConstantValues = true,
				           ExpectedMonotonicity = MonotonicityDirection.Increasing
			           };
			var runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		public void CanFindDifferingParts()
		{
			IFeatureClass featureClass = CreateLineClass(TestWorkspace);

			IPolycurve line1 =
				CurveConstruction.StartLine(0, 0, 5)
				                 .LineTo(1, 0, 6)
				                 .LineTo(1.5, 0, 6)
				                 .LineTo(2, 0, 6)
				                 .LineTo(3, 0, 7)
				                 .LineTo(3.2, 0, 6.8)
				                 .LineTo(3.5, 0, 6.5)
				                 .LineTo(4, 0, 6)
				                 .LineTo(5, 0, 7)
				                 .LineTo(6, 0, 7)
				                 .Curve;
			IFeature row1 = featureClass.CreateFeature();
			row1.Shape = line1;
			row1.Store();

			var test = new QaMonotonicZ(ReadOnlyTableFactory.Create(featureClass))
			           {
				           AllowConstantValues = false,
				           ExpectedMonotonicity = MonotonicityDirection.Increasing
			           };
			var runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(3, runner.Errors.Count);
		}

		[Test]
		public void CanUseFlipExpression()
		{
			IFeatureClass featureClass = CreateLineClass(TestWorkspace);

			IFeature row1 = featureClass.CreateFeature();
			row1.Shape = CurveConstruction.StartLine(0, 0, 5).LineTo(1, 0, 6).Curve;
			row1.set_Value(row1.Fields.FindField(_flipFieldName), 1);
			row1.Store();

			IFeature row2 = featureClass.CreateFeature();
			row2.Shape = CurveConstruction.StartLine(0, 0, 5).LineTo(1, 0, 6).Curve;
			row2.set_Value(row2.Fields.FindField(_flipFieldName), -1);
			row2.Store();

			IFeature row3 = featureClass.CreateFeature();
			row3.Shape = CurveConstruction.StartLine(0, 0, 6).LineTo(1, 0, 5).Curve;
			row3.Store();

			var test = new QaMonotonicZ(ReadOnlyTableFactory.Create(featureClass))
			           {
				           AllowConstantValues = false,
				           ExpectedMonotonicity = MonotonicityDirection.Decreasing,
				           FlipExpression = string.Format("{0} > 0", _flipFieldName)
			           };
			var runner = new QaTestRunner(test);
			runner.Execute();
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[NotNull]
		private static IFeatureClass CreateLineClass([NotNull] IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateIntegerField(_flipFieldName));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, true, false));

			Thread.Sleep(10);
			IFeatureClass featureClass =
				DatasetUtils.CreateSimpleFeatureClass(
					ws, "line_" + Environment.TickCount, fields, null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			return featureClass;
		}
	}
}

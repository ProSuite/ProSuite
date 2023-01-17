using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.IssueFilters;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test.IssueFilters
{
	[TestFixture]
	public class IssueFilterExpressionTest
	{
		private IFeatureClass _lineFc;
		private IFeatureClass _polyFc;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
			BuildData();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void ValidateNoFilter()
		{
			QaConstraint test = new QaConstraint(ReadOnlyTableFactory.Create(_lineFc), "1 = 0");

			var runner = new QaContainerTestRunner(1000, test);
			runner.Execute();
			Assert.AreEqual(4, runner.Errors.Count);
		}

		[Test]
		public void ValidateDefaultExpression()
		{
			QaConstraint test = new QaConstraint(ReadOnlyTableFactory.Create(_lineFc), "1 = 0");

			IIssueFilter ifWhere = new IfInvolvedRows("TEXT_FIELD = 'VAL1'");
			IIssueFilter ifIntersect = new IfIntersecting(ReadOnlyTableFactory.Create(_polyFc));

			IFilterEditTest filterTest = test;
			filterTest.SetIssueFilters(null, new[] { ifWhere, ifIntersect });

			var runner = new QaContainerTestRunner(1000, test);
			runner.Execute();

			Assert.AreEqual(1, runner.Errors.Count); // Filters combined by OR 
		}

		[Test]
		public void ValidateExpression()
		{
			QaConstraint test = new QaConstraint(ReadOnlyTableFactory.Create(_lineFc), "1 = 0");

			IIssueFilter ifWhere = new IfInvolvedRows("TEXT_FIELD = 'VAL1'")
			                       { Name = "ifWhere" };
			IIssueFilter ifIntersect = new IfIntersecting(ReadOnlyTableFactory.Create(_polyFc))
			                           { Name = "ifIntersect" };

			IFilterEditTest filterTest = test;
			filterTest.SetIssueFilters($"{ifWhere.Name} AND {ifIntersect.Name}",
			                           new[] { ifWhere, ifIntersect });

			var runner = new QaContainerTestRunner(1000, test);
			runner.Execute();

			Assert.AreEqual(3, runner.Errors.Count);
		}

		private void BuildData()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("IssueFilterTest");

			_lineFc = CreateFeatureClass(ws, "lineFc", esriGeometryType.esriGeometryPolyline,
			                             new List<IField>
			                             {
				                             FieldUtils.CreateTextField(
					                             "TEXT_FIELD", 100, "Some Text"),
				                             FieldUtils.CreateIntegerField(
					                             "NUMBER_FIELD", "Some Number")
			                             });

			int origTextFieldIdx = _lineFc.FindField("TEXT_FIELD");

			{
				IFeature f = _lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(20, 20).LineTo(70, 70).Curve;
				f.Value[origTextFieldIdx] = "VAL1";
				f.Store();
			}
			{
				IFeature f = _lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(10, 5).LineTo(2, 2).Curve;
				f.Value[origTextFieldIdx] = "VAL2";
				f.Store();
			}
			{
				IFeature f = _lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(7, 3).LineTo(4, 28).Curve;
				f.Value[origTextFieldIdx] = "VAL1";
				f.Store();
			}
			{
				IFeature f = _lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(20, 30).LineTo(30, 20).Curve;
				f.Value[origTextFieldIdx] = "VAL2";
				f.Store();
			}

			_polyFc = CreateFeatureClass(ws, "polyFc", esriGeometryType.esriGeometryPolygon);
			{
				IFeature f = _polyFc.CreateFeature();
				f.Shape = CurveConstruction.StartPoly(0, 0).LineTo(10, 0).LineTo(10, 10)
				                           .LineTo(0, 10).ClosePolygon();
				f.Store();
			}
		}

		private IFeatureClass CreateFeatureClass(IFeatureWorkspace ws, string name,
		                                         esriGeometryType geometryType,
		                                         IList<IField> customFields = null)
		{
			List<IField> fields = new List<IField>();
			fields.Add(FieldUtils.CreateOIDField());
			if (customFields != null)
			{
				fields.AddRange(customFields);
			}

			bool hasZ = geometryType == esriGeometryType.esriGeometryMultiPatch;
			fields.Add(FieldUtils.CreateShapeField(
				           "Shape", geometryType,
				           SpatialReferenceUtils.CreateSpatialReference
				           ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				            true), 1000, hasZ));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, name,
				FieldUtils.CreateFields(fields));
			return fc;
		}
	}
}

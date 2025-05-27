using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.IssueFilters;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test.IssueFilters
{
	[TestFixture]
	public class IfInvolvedRowsTest
	{
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
		public void CanFilterSingleTable()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("IssueFilterTest");

			IFeatureClass lineFc =
				CreateFeatureClass(ws, "lineFc", esriGeometryType.esriGeometryPolyline,
				                   new List<IField>
				                   {
					                   FieldUtils.CreateTextField("TEXT_FIELD", 100, "Some Text"),
					                   FieldUtils.CreateIntegerField("NUMBER_FIELD", "Some Number")
				                   });

			int origTextFieldIdx = lineFc.FindField("TEXT_FIELD");
			int origIntFieldIdx = lineFc.FindField("NUMBER_FIELD");

			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(0, 0).LineTo(70, 70).Curve;
				f.Value[origTextFieldIdx] = "VAL1";
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(10, 10).LineTo(60, 70)
				                           .Curve;
				f.Value[origTextFieldIdx] = "VAL2";
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(20, 20).LineTo(50, 70)
				                           .Curve;
				f.Value[origTextFieldIdx] = "VAL2";
				f.Value[origIntFieldIdx] = 42;
				f.Store();
			}

			ReadOnlyFeatureClass lineRoFc = ReadOnlyTableFactory.Create(lineFc);

			QaConstraint test = new QaConstraint(lineRoFc, "1 = 0");
			{
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(3, runner.Errors.Count);
			}
			{
				IfInvolvedRows i = new IfInvolvedRows("TEXT_FIELD = 'VAL1'");

				IFilterEditTest filterTest = test;
				filterTest.SetIssueFilters(null, new IIssueFilter[] { i });

				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();

				Assert.AreEqual(2, runner.Errors.Count);
			}
		}

		[Test]
		public void CanFilterSingleTableWithTablesParameterSet()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("IssueFilterTest");

			IFeatureClass lineFc =
				CreateFeatureClass(ws, "lineFc", esriGeometryType.esriGeometryPolyline,
				                   new List<IField>
				                   {
					                   FieldUtils.CreateTextField("TEXT_FIELD", 100, "Some Text"),
					                   FieldUtils.CreateIntegerField("NUMBER_FIELD", "Some Number")
				                   });

			int origTextFieldIdx = lineFc.FindField("TEXT_FIELD");
			int origIntFieldIdx = lineFc.FindField("NUMBER_FIELD");

			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(0, 0).LineTo(70, 70).Curve;
				f.Value[origTextFieldIdx] = "VAL1";
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(10, 10).LineTo(60, 70)
				                           .Curve;
				f.Value[origTextFieldIdx] = "VAL2";
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(20, 20).LineTo(50, 70)
				                           .Curve;
				f.Value[origTextFieldIdx] = "VAL2";
				f.Value[origIntFieldIdx] = 42;
				f.Store();
			}

			ReadOnlyFeatureClass lineRoFc = ReadOnlyTableFactory.Create(lineFc);

			QaConstraint test = new QaConstraint(lineRoFc, "1 = 0");
			{
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(3, runner.Errors.Count);
			}
			{
				IfInvolvedRows i = new IfInvolvedRows("TEXT_FIELD = 'VAL1'")
				                   {
					                   Tables = new List<IReadOnlyTable> { lineRoFc }
				                   };

				IFilterEditTest filterTest = test;
				filterTest.SetIssueFilters(null, new IIssueFilter[] { i });

				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();

				Assert.AreEqual(2, runner.Errors.Count);
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

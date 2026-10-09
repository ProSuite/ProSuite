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
	public class IfIssueConstraintTest
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
		public void CanFilterByIssueCode()
		{
			QaConstraint test = CreateTestWithThreeErrors();

			AssertErrorCount(3, test);

			SetIssueFilter(test, "$IssueCode LIKE '%ConstraintNotFulfilled'");

			AssertErrorCount(0, test);
		}

		[Test]
		public void CanFilterByIssueGeometry()
		{
			QaConstraint test = CreateTestWithThreeErrors();

			AssertErrorCount(3, test);

			// only the first line is longer than 90
			SetIssueFilter(test, "$Length > 90");

			AssertErrorCount(2, test);
		}

		[Test]
		public void CanFilterByIssueCodeAndGeometry()
		{
			QaConstraint test = CreateTestWithThreeErrors();

			AssertErrorCount(3, test);

			SetIssueFilter(test,
			               "$IssueCode LIKE '%ConstraintNotFulfilled' AND $Length > 90");

			AssertErrorCount(2, test);
		}

		[Test]
		public void CanFilterNothingIfConstraintNeverFulfilled()
		{
			QaConstraint test = CreateTestWithThreeErrors();

			SetIssueFilter(test, "$IssueCode = 'DoesNotExist'");

			AssertErrorCount(3, test);
		}

		#region Test utils

		private static void SetIssueFilter(IFilterEditTest test, string constraint)
		{
			test.SetIssueFilters(null,
			                     new IIssueFilter[] { new IfIssueConstraint(constraint) });
		}

		private static void AssertErrorCount(int expected, ITest test)
		{
			var runner = new QaContainerTestRunner(1000, test);
			runner.Execute();

			Assert.AreEqual(expected, runner.Errors.Count);
		}

		private static QaConstraint CreateTestWithThreeErrors()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("IfIssueConstraintTest");

			IFeatureClass lineFc =
				CreateFeatureClass(ws, "lineFc", esriGeometryType.esriGeometryPolyline,
				                   new List<IField>
				                   {
					                   FieldUtils.CreateTextField("TEXT_FIELD", 100, "Some Text")
				                   });

			int textFieldIdx = lineFc.FindField("TEXT_FIELD");

			{
				// length ~ 98.99
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(0, 0).LineTo(70, 70).Curve;
				f.Value[textFieldIdx] = "VAL1";
				f.Store();
			}
			{
				// length ~ 78.10
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(10, 10).LineTo(60, 70).Curve;
				f.Value[textFieldIdx] = "VAL2";
				f.Store();
			}
			{
				// length ~ 58.31
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(20, 20).LineTo(50, 70).Curve;
				f.Value[textFieldIdx] = "VAL2";
				f.Store();
			}

			ReadOnlyFeatureClass lineRoFc = ReadOnlyTableFactory.Create(lineFc);

			// the constraint is never fulfilled: every row produces an issue
			return new QaConstraint(lineRoFc, "1 = 0");
		}

		private static IFeatureClass CreateFeatureClass(IFeatureWorkspace ws, string name,
		                                                esriGeometryType geometryType,
		                                                IList<IField> customFields = null)
		{
			var fields = new List<IField> { FieldUtils.CreateOIDField() };

			if (customFields != null)
			{
				fields.AddRange(customFields);
			}

			fields.Add(FieldUtils.CreateShapeField(
				           "Shape", geometryType,
				           SpatialReferenceUtils.CreateSpatialReference(
					           (int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true),
				           1000));

			return DatasetUtils.CreateSimpleFeatureClass(ws, name,
			                                             FieldUtils.CreateFields(fields));
		}

		#endregion
	}
}

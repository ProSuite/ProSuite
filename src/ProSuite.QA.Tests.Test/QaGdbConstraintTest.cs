using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Tests.Test.TestRunners;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.QA.Tests.Test.Construction;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaGdbConstraintTest
	{
		private IFeatureWorkspace _testWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();

			_testWs = TestWorkspaceUtils.CreateTestFgdbWorkspace(
				"QaGdbConstraintTest");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void ValidateTableDomains()
		{
			string tableName = "table1";
			IList<IField> domainFields = GetDomainFields(tableName);
			ITable table = DatasetUtils.CreateTable(
				_testWs, tableName,
				FieldUtils.CreateOIDField(), domainFields[0], domainFields[1]);

			int cvFieldIndex = table.FindField(domainFields[0].Name);
			int rangeFieldIndex = table.FindField(domainFields[1].Name);

			var row1 = table.CreateRow();
			row1.Store();

			var row2 = table.CreateRow();
			row2.Value[cvFieldIndex] = 2; // valid in domain
			row2.Value[rangeFieldIndex] = 50; // valid in domain
			row2.Store();

			var row3 = table.CreateRow();
			row3.Value[cvFieldIndex] = 4; // not valid for domain
			row3.Value[rangeFieldIndex] = 200; // valid in domain
			row3.Store();

			{
				IReadOnlyTable t = ReadOnlyTableFactory.Create(table);
				QaGdbConstraint test = new QaGdbConstraint(t);

				var runner = new QaContainerTestRunner(1000, test);

				runner.Execute();

				Assert.AreEqual(1, runner.Errors.Count);
			}
		}

		[Test]
		public void ValidateFeatureClassDomains()
		{
			string tableName = "fc1";
			IList<IField> domainFields = GetDomainFields(tableName);
			IList<IField> allFields =
				new List<IField>
				{
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPolyline,
					                            SpatialReferenceUtils.CreateSpatialReference(2056)),
					domainFields[0], domainFields[1]
				};
			IFeatureClass table = DatasetUtils.CreateSimpleFeatureClass(
				_testWs, tableName, FieldUtils.CreateFields(allFields));

			int cvFieldIndex = table.FindField(domainFields[0].Name);
			int rangeFieldIndex = table.FindField(domainFields[1].Name);

			{
				var row1 = table.CreateFeature();
				row1.Shape = CurveConstruction.StartLine(2600000, 1200000).Line(10, 10).Curve;
				row1.Store();
			}
			{
				var row2 = table.CreateFeature();
				row2.Value[cvFieldIndex] = 2; // valid in domain
				row2.Value[rangeFieldIndex] = 50; // valid in domain
				row2.Shape = CurveConstruction.StartLine(2650000, 1200000).Line(10, 10).Curve;
				row2.Store();
			}
			{
				var row3 = table.CreateFeature();
				row3.Value[cvFieldIndex] = 4; // not valid for domain
				row3.Value[rangeFieldIndex] = 200; // valid in domain
				row3.Shape = CurveConstruction.StartLine(2700000, 1200000).Line(10, 10).Curve;
				row3.Store();
			}

			IReadOnlyFeatureClass roFc = ReadOnlyTableFactory.Create(table);
			{
				QaGdbConstraint test = new QaGdbConstraint(roFc);
				var runner = new QaContainerTestRunner(1000, test);

				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
			{
				// Check with cached rows
				QaGdbConstraint test0 = new QaGdbConstraint(roFc);
				QaCrossesSelf test1 = new QaCrossesSelf(roFc);
				var runner = new QaContainerTestRunner(1000, test0, test1);

				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
		}

		[NotNull]
		private IList<IField> GetDomainFields(
			[NotNull] string tableName)
		{
			IRangeDomain rangeDomain = DomainUtils.CreateRangeDomain(
				tableName + "_range",
				esriFieldType.esriFieldTypeInteger, 0, 100);
			DomainUtils.AddDomain(_testWs, rangeDomain);

			ICodedValueDomain cvDomain = DomainUtils.CreateCodedValueDomain(
				tableName + "_cv",
				esriFieldType.esriFieldTypeInteger, null,
				esriSplitPolicyType.esriSPTDuplicate,
				esriMergePolicyType.esriMPTDefaultValue,
				new CodedValue(1, "Value 1"),
				new CodedValue(2, "Value 2"),
				new CodedValue(3, "Value 3"));

			DomainUtils.AddDomain(_testWs, cvDomain);

			IField cvField = FieldUtils.CreateIntegerField("CvField");
			((IFieldEdit)cvField).Domain_2 = (IDomain)cvDomain;

			IField rangeField = FieldUtils.CreateIntegerField("RangeField");
			((IFieldEdit)rangeField).Domain_2 = (IDomain)rangeDomain;

			List<IField> fields = new List<IField>
			                      { cvField, rangeField };

			return fields;
		}

	}
}

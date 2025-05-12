using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.TestFactories;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaGdbConstraintFactoryTest
	{
		private IFeatureWorkspace _testWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();

			_testWs = TestWorkspaceUtils.CreateTestFgdbWorkspace(
				"QaGdbConstraintFactoryTest");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanFindNullValuesInFieldUsingCodedValueDomain()
		{
			int cvFieldIndex;
			int rangeFieldIndex;
			ITable table = CreateTestTable("table1",
			                               out cvFieldIndex,
			                               out rangeFieldIndex);

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

			var model = new SimpleModel("model", table);
			var tableDataset = model.AddDataset(
				new ModelTableDataset(DatasetUtils.GetName(table)));

			var condition = new QualityCondition(
				"condition",
				new TestDescriptor("testdescriptor",
				                   new ClassDescriptor(typeof(QaGdbConstraintFactory))));

			InstanceConfigurationUtils.AddParameterValue(condition, "table", tableDataset);
			InstanceConfigurationUtils.AddParameterValue(
				condition, "AllowNullValuesForCodedValueDomains", false);

			{
				var factory = new QaGdbConstraintFactory { Condition = condition };

				IList<ITest> tests =
					factory.CreateTests(
						new SimpleDatasetOpener(model.GetMasterDatabaseWorkspaceContext()));

				Assert.AreEqual(1, tests.Count);

				var runner = new QaTestRunner(tests[0]);

				runner.Execute();

				Assert.AreEqual(3, runner.Errors.Count);
				Assert.AreEqual(
					"Domain table1_cv - Invalid value combination: CVFIELD = <null>",
					runner.Errors[0].Description);
				Assert.AreEqual(
					"Domain table1_cv - Invalid value combination: CVFIELD = 4",
					runner.Errors[1].Description);
				Assert.AreEqual(
					"Domain table1_range - Invalid value combination: RANGEFIELD = 200",
					runner.Errors[2].Description);
			}

			{
				QualityCondition condWithFields = (QualityCondition) condition.CreateCopy();
				InstanceConfigurationUtils.AddParameterValue(
					condWithFields, "Fields", "CvField");

				var factory = new QaGdbConstraintFactory { Condition = condWithFields };

				IList<ITest> tests =
					factory.CreateTests(
						new SimpleDatasetOpener(model.GetMasterDatabaseWorkspaceContext()));

				Assert.AreEqual(1, tests.Count);
				var runner = new QaTestRunner(tests[0]);
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);
			}
			{
				QualityCondition condWithFields = (QualityCondition) condition.CreateCopy();
				InstanceConfigurationUtils.AddParameterValue(
					condWithFields, "Fields", "CvField");
				InstanceConfigurationUtils.AddParameterValue(
					condWithFields, "Fields", "RangeField");

				var factory = new QaGdbConstraintFactory { Condition = condWithFields };

				IList<ITest> tests =
					factory.CreateTests(
						new SimpleDatasetOpener(model.GetMasterDatabaseWorkspaceContext()));

				Assert.AreEqual(1, tests.Count);
				var runner = new QaTestRunner(tests[0]);
				runner.Execute();
				Assert.AreEqual(3, runner.Errors.Count);
			}
		}

		[Test]
		public void CanFindNullValuesInFieldUsingRangeDomain()
		{
			int cvFieldIndex;
			int rangeFieldIndex;
			ITable table = CreateTestTable("table2",
			                               out cvFieldIndex,
			                               out rangeFieldIndex);

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

			var model = new SimpleModel("model", table);
			var tableDataset = model.AddDataset(
				new ModelTableDataset(DatasetUtils.GetName(table)));

			var condition = new QualityCondition(
				"condition",
				new TestDescriptor("testdescriptor",
				                   new ClassDescriptor(typeof(QaGdbConstraintFactory))));

			InstanceConfigurationUtils.AddParameterValue(condition, "table", tableDataset);
			InstanceConfigurationUtils.AddParameterValue(
				condition, "AllowNullValuesForRangeDomains", false);

			var factory = new QaGdbConstraintFactory { Condition = condition };

			IList<ITest> tests =
				factory.CreateTests(new SimpleDatasetOpener(model.GetMasterDatabaseWorkspaceContext()));

			Assert.AreEqual(1, tests.Count);

			var runner = new QaTestRunner(tests[0]);

			runner.Execute();

			Assert.AreEqual(3, runner.Errors.Count);
			Assert.AreEqual(
				"Domain table2_range - Invalid value combination: RANGEFIELD = <null>",
				runner.Errors[0].Description);
			Assert.AreEqual(
				"Domain table2_cv - Invalid value combination: CVFIELD = 4",
				runner.Errors[1].Description);
			Assert.AreEqual(
				"Domain table2_range - Invalid value combination: RANGEFIELD = 200",
				runner.Errors[2].Description);
		}

		[Test]
		public void CanIgnoreNullValues()
		{
			int cvFieldIndex;
			int rangeFieldIndex;
			ITable table = CreateTestTable(
				"table3", out cvFieldIndex, out rangeFieldIndex);

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

			var model = new SimpleModel("model", table);
			var tableDataset = model.AddDataset(
				new ModelTableDataset(DatasetUtils.GetName(table)));

			var condition = new QualityCondition(
				"condition",
				new TestDescriptor("testdescriptor",
				                   new ClassDescriptor(typeof(QaGdbConstraintFactory))));

			InstanceConfigurationUtils.AddParameterValue(condition, "table", tableDataset);

			var factory = new QaGdbConstraintFactory { Condition = condition };

			IList<ITest> tests =
				factory.CreateTests(new SimpleDatasetOpener(model.GetMasterDatabaseWorkspaceContext()));

			Assert.AreEqual(1, tests.Count);

			var runner = new QaTestRunner(tests[0]);

			runner.Execute();

			Assert.AreEqual(2, runner.Errors.Count);
			Assert.AreEqual(
				"Domain table3_cv - Invalid value combination: CVFIELD = 4",
				runner.Errors[0].Description);
			Assert.AreEqual(
				"Domain table3_range - Invalid value combination: RANGEFIELD = 200",
				runner.Errors[1].Description);
		}

		[NotNull]
		private ITable CreateTestTable(
			[NotNull] string tableName, out int cvFieldIndex,
			out int rangeFieldIndex, IList<IField> specialFields = null)
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
			((IFieldEdit) cvField).Domain_2 = (IDomain) cvDomain;

			IField rangeField = FieldUtils.CreateIntegerField("RangeField");
			((IFieldEdit) rangeField).Domain_2 = (IDomain) rangeDomain;

			List<IField> fields = new List<IField>
			                      { FieldUtils.CreateOIDField(), cvField, rangeField };
			if (specialFields != null)
			{
				fields.AddRange(specialFields);
			}

			ITable table = DatasetUtils.CreateTable(_testWs,
			                                        tableName,
			                                        fields.ToArray());

			cvFieldIndex = table.FindField(cvField.Name);
			rangeFieldIndex = table.FindField(rangeField.Name);

			return table;
		}
	}
}

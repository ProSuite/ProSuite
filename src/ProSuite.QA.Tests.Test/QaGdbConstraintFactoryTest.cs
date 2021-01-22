using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Test;
using ProSuite.QA.TestFactories;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.AO.QA;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaGdbConstraintFactoryTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _testWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);

			_testWs = TestWorkspaceUtils.CreateTestFgdbWorkspace(
				"QaGdbConstraintFactoryTest");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
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

			QualityCondition_Utils.AddParameterValue(condition, "table", tableDataset);
			QualityCondition_Utils.AddParameterValue(condition,
			                                        "AllowNullValuesForCodedValueDomains", false);

			var factory = new QaGdbConstraintFactory {Condition = condition};

			IList<ITest> tests = factory.CreateTests(new SimpleDatasetOpener(model.MasterDatabaseWorkspaceContext));

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

			QualityCondition_Utils.AddParameterValue(condition, "table", tableDataset);
			QualityCondition_Utils.AddParameterValue(condition, "AllowNullValuesForRangeDomains",
			                                        false);

			var factory = new QaGdbConstraintFactory {Condition = condition};

			IList<ITest> tests = factory.CreateTests(new SimpleDatasetOpener(model.MasterDatabaseWorkspaceContext));

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
			ITable table = CreateTestTable("table3",
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

			QualityCondition_Utils.AddParameterValue(condition, "table", tableDataset);

			var factory = new QaGdbConstraintFactory {Condition = condition};

			IList<ITest> tests = factory.CreateTests(new SimpleDatasetOpener(model.MasterDatabaseWorkspaceContext));

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
		private ITable CreateTestTable([NotNull] string tableName, out int cvFieldIndex,
		                               out int rangeFieldIndex)
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

			ITable table = DatasetUtils.CreateTable(_testWs,
			                                        tableName,
			                                        FieldUtils.CreateOIDField(),
			                                        cvField,
			                                        rangeField);

			cvFieldIndex = table.FindField(cvField.Name);
			rangeFieldIndex = table.FindField(rangeField.Name);

			return table;
		}
	}
}

using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.Geodatabase;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.Test.QA
{
	internal static class VerificationTestUtils
	{
		internal static QualitySpecification CreateQualitySpecification(
			string featureClassName,
			string specificationName,
			string gdbPath)
		{
			const string simpleGeometryDescriptorName = "SimpleGeometry(0)";
			TestDescriptor simpleGeometryDescriptor = new TestDescriptor(
				simpleGeometryDescriptorName,
				new ClassDescriptor(
					"ProSuite.QA.Tests.QaSimpleGeometry",
					"ProSuite.QA.Tests"),
				0);

			const string gdbConstraintsDescriptorName = "GdbConstraintFactory";
			TestDescriptor gdbConstraintsDescriptor = new TestDescriptor(
				gdbConstraintsDescriptorName,
				new ClassDescriptor(
					"ProSuite.QA.TestFactories.QaGdbConstraintFactory",
					"ProSuite.QA.TestFactories"));

			Model model = new TestModel("testModel");
			model.UserConnectionProvider = new FileGdbConnectionProvider(gdbPath);
			TestDataset lineDataset = model.AddDataset(new TestDataset(featureClassName));

			var condition1 =
				new QualityCondition("condition1", simpleGeometryDescriptor);
			InstanceConfigurationUtils.AddParameterValue(condition1, "featureClass", lineDataset);

			var condition2 =
				new QualityCondition("condition2", gdbConstraintsDescriptor);

			InstanceConfigurationUtils.AddParameterValue(condition2, "table", lineDataset,
			                                             "[OBJEKTART] IS NOT NULL");
			InstanceConfigurationUtils.AddScalarParameterValue(
				condition2, "AllowNullValuesForCodedValueDomains", true);

			QualitySpecification specification = new QualitySpecification(specificationName);
			specification.AddElement(condition1);
			specification.AddElement(condition2);

			return specification;
		}

		private class TestModel : ProductionModel, IModelMasterDatabase
		{
			public TestModel(string name) : base(name) { }

			public override string QualifyModelElementName(string modelElementName)
			{
				return ModelUtils.QualifyModelElementName(this, modelElementName);
			}

			public override string TranslateToModelElementName(string masterDatabaseDatasetName)
			{
				return ModelUtils.TranslateToModelElementName(this, masterDatabaseDatasetName);
			}

			IWorkspaceContext IModelMasterDatabase.CreateMasterDatabaseWorkspaceContext()
			{
				return ModelUtils.CreateDefaultMasterDatabaseWorkspaceContext(this);
			}
		}

		private class TestDataset : VectorDataset
		{
			public TestDataset([NotNull] string name) : base(name) { }
		}
	}
}

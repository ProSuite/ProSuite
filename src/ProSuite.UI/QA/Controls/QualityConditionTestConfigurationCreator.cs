using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.QA.Controls;
using ProSuite.UI.QA.PropertyEditors;

namespace ProSuite.UI.QA.Controls
{
	public class QualityConditionTestConfigurationCreator : ITestConfigurationCreator
	{
		public ITestConfigurator CreateTestConfiguration(QualityCondition condition, bool readOnly)
		{
			TestFactory factory = TestFactoryUtils.CreateTestFactory(condition);
			if (factory == null)
			{
				return null;
			}

			if (readOnly)
			{
				condition = (QualityCondition) condition.CreateCopy();
			}

			TestParameterValueUtils.SyncParameterValues(condition);

			TestDescriptor testDescriptor = condition.TestDescriptor;

			string testAssemblyName = testDescriptor != null &&
			                          (testDescriptor.TestClass != null ||
			                           testDescriptor.TestFactoryDescriptor != null)
				                          ? testDescriptor.AssemblyName
				                          : null;

			ITestConfigurator testConfigurator =
				DefaultTestConfiguratorFactory.CreateTestConfigurator(
					"ConfigTest", factory.Parameters, testAssemblyName, readOnly);

			return testConfigurator;
		}
	}
}

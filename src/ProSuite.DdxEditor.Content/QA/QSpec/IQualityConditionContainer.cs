using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.InstanceConfig;
using ProSuite.DdxEditor.Content.QA.QCon;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public interface IQualityConditionContainer : IInstanceConfigurationContainer
	{
		[NotNull]
		QualityConditionItem CreateQualityConditionItem(
			[NotNull] IInstanceConfigurationContainerItem containerItem);
	}
}

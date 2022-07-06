using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Content.QA.QSpec;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	public interface IInstanceConfigurationContainer
	{
		[NotNull]
		IEnumerable<Item> GetInstanceConfigurationItems(
			[NotNull] IInstanceConfigurationContainerItem containerItem);

		[NotNull]
		IEnumerable<InstanceConfigurationDatasetTableRow>
			GetInstanceConfigurationDatasetTableRows<T>() where T : InstanceConfiguration;

		//[NotNull]
		//IEnumerable<QualityConditionInCategoryTableRow> GetQualityConditionTableRows();

		//[NotNull]
		//QualityConditionItem CreateQualityConditionItem(
		//	[NotNull] IInstanceConfigurationContainerItem containerItem);
	}
}

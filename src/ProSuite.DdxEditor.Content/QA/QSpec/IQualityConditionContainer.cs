using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public interface IQualityConditionContainer
	{
		[NotNull]
		IEnumerable<Item> GetQualityConditionItems(
			[NotNull] IQualityConditionContainerItem containerItem);

		[NotNull]
		IEnumerable<QualityConditionDatasetTableRow> GetQualityConditionDatasetTableRows();

		[NotNull]
		IEnumerable<QualityConditionInCategoryTableRow> GetQualityConditionTableRows();

		[NotNull]
		QualityConditionItem CreateQualityConditionItem(
			[NotNull] IQualityConditionContainerItem containerItem);
	}
}

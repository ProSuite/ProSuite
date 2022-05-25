using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.UI.QA.Controls;

public interface IQualityConditionTableViewControl
{
	void BindToParameterValues(
		[NotNull] BindingList<ParameterValueListItem> parameterValueItems);

	void NotifySavedChanges(QualityCondition qualityCondition);
}

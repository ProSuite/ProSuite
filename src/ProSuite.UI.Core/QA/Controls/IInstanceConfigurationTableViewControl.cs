using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.UI.Core.QA.Controls
{
	public interface IInstanceConfigurationTableViewControl
	{
		void BindToParameterValues(
			[NotNull] BindingList<ParameterValueListItem> parameterValueItems);

		void BindTo([NotNull] InstanceConfiguration instanceConfiguration);
	}
}

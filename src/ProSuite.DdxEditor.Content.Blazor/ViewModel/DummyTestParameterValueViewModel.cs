using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor.View;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public class DummyTestParameterValueViewModel : ViewModelBase
{
	public DummyTestParameterValueViewModel([NotNull] TestParameter parameter,
	                                        [NotNull] IInstanceConfigurationViewModel observer) :
		base(parameter, "Click to add new row.", observer)
	{
		ComponentParameters.Add("ViewModel", this);

		ComponentType = typeof(StringValueBlazor);
	}
}

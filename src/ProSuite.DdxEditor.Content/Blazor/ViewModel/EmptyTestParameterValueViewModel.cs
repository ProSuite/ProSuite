using ProSuite.DdxEditor.Content.Blazor.View;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public class EmptyTestParameterValueViewModel : ViewModelBase
{
	public EmptyTestParameterValueViewModel()
	{
		Value = "empty value";
		
		ComponentType = typeof(DatasetTestParameterValueBlazor);
		ComponentParameters.Add("ViewModel", this);
	}

	public override object Value { get; set; }
}

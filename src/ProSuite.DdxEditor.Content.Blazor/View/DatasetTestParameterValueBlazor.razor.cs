using ProSuite.DdxEditor.Content.Blazor.ViewModel;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public partial class DatasetTestParameterValueBlazor : TestParameterValueBlazorBase<string>
{
	public DatasetTestParameterValueViewModel DatasetViewModel =>
		(DatasetTestParameterValueViewModel) ViewModel;
	
	private void OnClick()
	{
		DatasetViewModel.FindDatasetClicked();
	}

	private void OnLinkClicked()
	{
		DatasetViewModel.GoTo();
	}
}

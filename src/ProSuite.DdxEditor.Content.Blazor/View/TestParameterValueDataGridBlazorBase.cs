using Microsoft.AspNetCore.Components;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;
using Radzen;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public abstract class TestParameterValueDataGridBlazorBase : DataGridBlazorBase
{
	[NotNull]
	[Parameter]
	// ReSharper disable once NotNullMemberIsNotInitialized
	public TestParameterValueCollectionViewModel ViewModel { get; set; }

	protected void OnLoadData(LoadDataArgs args)
	{
		Rows = Assert.NotNull(ViewModel).Values;
	}

	protected void OnUpClicked()
	{
		Assert.NotNull(SelectedRow);
		ViewModel.MoveUp(SelectedRow);

		DataGrid.Reload();
	}

	protected void OnDownClicked()
	{
		Assert.NotNull(SelectedRow);
		ViewModel.MoveDown(SelectedRow);

		DataGrid.Reload();
	}

	protected void OnDeleteClicked()
	{
		Assert.NotNull(SelectedRow);

		ViewModelBase newSelectedRow;
		ViewModel.Remove(SelectedRow, out newSelectedRow);

		DataGrid.Reload();

		DataGrid.SelectRow(newSelectedRow);
	}

	protected override void OnRowClickCore(ViewModelBase row)
	{
		if (row is DummyTestParameterValueViewModel)
		{
			ViewModelBase insertedRow = ViewModel.InsertDefaultRow();

			// a row has been inserted > reload data grid
			DataGrid.Reload();

			base.OnRowClickCore(insertedRow);

			// select inserted row
			DataGrid.SelectRow(insertedRow);
		}
		else
		{
			base.OnRowClickCore(row);
		}
	}
}

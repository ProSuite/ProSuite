using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using Radzen;
using Radzen.Blazor;

namespace ProSuite.DdxEditor.Content.Blazor;

public partial class QualityConditionTableViewBlazor : IDisposable
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private RadzenDataGrid<TestParameterViewModel> _testParametersGrid;

	[NotNull]
	[Parameter]
	// ReSharper disable once NotNullMemberIsNotInitialized
	public QualityConditionViewModel Model { get; set; }

	public IEnumerable<TestParameterViewModel> Items => Model.Items;

	public TestParameterViewModel SelectedItem { get; set; }

	public void Dispose()
	{
		Model.SavedChanges -= OnSavedChanges;
	}

	protected override void OnInitialized()
	{
		// 1
		Model.SavedChanges += OnSavedChanges;

		base.OnInitialized();
	}

	private void OnSavedChanges(object sender, EventArgs e)
	{
		_testParametersGrid.UpdateRow(SelectedItem);
	}

	private async void OnRowClick(DataGridRowMouseEventArgs<TestParameterViewModel> arg)
	{
		SelectedItem = arg.Data;

		await EditRow(arg.Data);
	}

	private async Task EditRow(TestParameterViewModel item)
	{
		await _testParametersGrid.EditRow(item);
	}

	private void OnChange(object o) { }

	private async void ValueButtonClicked(TestParameterViewModel item)
	{
		Model.Clicked();
	}
}

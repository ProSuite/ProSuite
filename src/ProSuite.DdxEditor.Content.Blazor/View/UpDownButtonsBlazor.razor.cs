using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;
using Radzen.Blazor;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public partial class UpDownButtonsBlazor
{
	[Parameter]
	public RadzenDataGrid<ViewModelBase> Grid { get; set; }

	[CanBeNull]
	public ViewModelBase Selected => Grid.Value.LastOrDefault();

	[Parameter]
	public EventCallback<ViewModelBase> SelectedChanged { get; set; }

	[Parameter]
	public EventCallback<EventArgs> OnUpClicked { get; set; }

	[Parameter]
	public EventCallback<EventArgs> OnDownClicked { get; set; }

	[Parameter]
	public EventCallback<EventArgs> OnDeleteClicked { get; set; }

	private void UpClicked()
	{
		Assert.NotNull(Selected);
		Assert.NotNull(Grid);

		OnUpClicked.InvokeAsync(EventArgs.Empty);
	}

	private void DownClicked()
	{
		Assert.NotNull(Selected);
		Assert.NotNull(Grid);

		OnDownClicked.InvokeAsync(EventArgs.Empty);
	}

	private void DeleteRowClicked()
	{
		Assert.NotNull(Selected);
		Assert.NotNull(Grid);

		OnDeleteClicked.InvokeAsync(EventArgs.Empty);
	}

	private bool ButtonDisabledCore([CanBeNull] List<ViewModelBase> rows)
	{
		if (rows == null || rows.Count == 0)
		{
			return true;
		}

		if (Selected is null or DummyTestParameterValueViewModel)
		{
			return true;
		}

		return false;
	}

	private bool UpButtonDisabled()
	{
		List<ViewModelBase> rows = Grid?.Data.ToList();

		if (ButtonDisabledCore(rows))
		{
			return true;
		}

		// is Selected the first row?
		return rows?.IndexOf(Selected) == 0;
	}

	private bool DownButtonDisabled()
	{
		List<ViewModelBase> rows = Grid?.Data.ToList();

		if (ButtonDisabledCore(rows))
		{
			return true;
		}

		// Is Selected the 2nd last row? The last row is always
		// a dummy row.
		return rows?.Count - 2 == rows?.IndexOf(Selected);
	}

	private bool DeleteRowButtonDisabled()
	{
		List<ViewModelBase> rows = Grid?.Data.ToList();

		if (ButtonDisabledCore(rows))
		{
			return true;
		}

		if (Selected == null)
		{
			return true;
		}

		// NOTE: Even though a field is required, the list can be empty. If no empty list is allowed
		//       some validation on the condition level should be used.

		return false;
	}
}

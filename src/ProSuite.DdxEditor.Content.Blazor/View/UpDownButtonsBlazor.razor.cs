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
	[Parameter]
	public ViewModelBase Selected { get; set; }

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

		// todo daro to early?
		Grid.Reload();
	}

	private void DownClicked()
	{
		Assert.NotNull(Selected);
		Assert.NotNull(Grid);

		OnDownClicked.InvokeAsync(EventArgs.Empty);

		// todo daro to early?
		Grid.Reload();
	}

	private void DeleteRowClicked()
	{
		Assert.NotNull(Selected);
		Assert.NotNull(Grid);

		OnDeleteClicked.InvokeAsync(EventArgs.Empty);

		Grid.Reload();

		Selected = null;
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

		// is Selected the last row?
		return rows?.Count - 1 == rows?.IndexOf(Selected);
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

		return false;
	}
}

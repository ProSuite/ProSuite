using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Misc;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using Radzen;
using Radzen.Blazor;

namespace ProSuite.DdxEditor.Content.Blazor;

public partial class QualityConditionTableViewBlazor : IDisposable
{
	// ReSharper disable once NotNullMemberIsNotInitialized
	[NotNull] private RadzenDataGrid<ViewModelBase> _mainGrid;

	[NotNull] private List<RadzenDataGrid<ViewModelBase>> _childGrids = new();

	// ReSharper disable once NotNullMemberIsNotInitialized
	[NotNull] private IInstanceConfigurationAwareViewModel _viewModel;

	private Latch _latch = new Latch();

	[NotNull]
	[Parameter]
	// ReSharper disable once NotNullMemberIsNotInitialized
	public IInstanceConfigurationAwareViewModel ViewModel
	{
		get => _viewModel;
		set
		{
			_viewModel = value;

			_viewModel.PropertyChanged += OnPropertyChanged;
		}
	}

	private IList<ViewModelBase> Rows => ViewModel.Rows;

	public ViewModelBase SelectedRow { get; set; }
	// todo rename to SelectedChildRow
	public ViewModelBase SelectedCollectionRow { get; set; }

	// this is necessary that data grid shows selection
	public IList<ViewModelBase> SelectedRows { get; set; } = new List<ViewModelBase>();

	public void Dispose()
	{
		_viewModel.PropertyChanged -= OnPropertyChanged;
	}

	private async void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		StateHasChanged();

		foreach (ViewModelBase row in Rows)
		{
			await _mainGrid.UpdateRow(row);
		}

		if (_childGrids.Count == 0)
		{
			return;
		}

		foreach (ViewModelBase row in Rows.OfType<TestParameterValueCollectionViewModel>()
										  .SelectMany(row => row.Values))
		{
			await DataGridUtils.UpdateRowIfNotNull(_childGrids, row);
		}
	}

	private async void OnRowClick(DataGridRowMouseEventArgs<ViewModelBase> args)
	{
		Assert.True(_mainGrid.Data.Contains(args.Data), "row is not from grid");
		
		await DataGridUtils.UpdateRowIfNotNull(_childGrids, SelectedCollectionRow);

		ViewModelBase recent = SelectedRow;

		SelectedRow = args.Data;
		SelectedCollectionRow = null;

		if (SelectedRow is TestParameterValueCollectionViewModel)
		{
			await DataGridUtils.UpdateRowIfNotNull(_mainGrid, recent);
			return;
		}
		
		if (! Equals(recent, SelectedRow))
		{
			await DataGridUtils.UpdateRowIfNotNull(_mainGrid, recent);

			await DataGridUtils.EditRow(_mainGrid, SelectedRow);
		}
	}

	private async void OnChildRowClick(DataGridRowMouseEventArgs<ViewModelBase> args)
	{
		await DataGridUtils.UpdateRowIfNotNull(_mainGrid, SelectedRow);

		ViewModelBase recent = SelectedCollectionRow;

		SelectedCollectionRow = args.Data;
		SelectedRow = null;

		if (! Equals(recent, SelectedCollectionRow))
		{
			await DataGridUtils.UpdateRowIfNotNull(_childGrids, recent);

			if (SelectedCollectionRow.New)
			{
				await DataGridUtils.UpdateRow(_childGrids, SelectedCollectionRow);
			}

			await DataGridUtils.EditRow(_childGrids, SelectedCollectionRow);
		}
	}

	#region layout

	#region row

	private void OnRowCreate(ViewModelBase row)
	{
		row.New = false;
	}
	
	private void OnRowUpdate(ViewModelBase row)
	{
		row.Dirty = false;
	}

	private void OnRowRender(RowRenderEventArgs<ViewModelBase> args)
	{
		// todo daro inline
		bool isExpandable = args.Data.Values != null;
		
		args.Expandable = isExpandable;
	}

	private void OnRowExpand(ViewModelBase row)
	{
		if (_latch.IsLatched)
		{
			return;
		}

		row.Expanded = true;
	}

	private void OnRowCollapse(ViewModelBase row)
	{
		// OnChildGridRender fires every time
		_childGrids.Clear();

		if (_latch.IsLatched)
		{
			return;
		}

		row.Expanded = false;
	}

	#endregion

	private async void OnRender(DataGridRenderEventArgs<ViewModelBase> args)
	{
		RadzenDataGrid<ViewModelBase> grid = args.Grid;

		foreach (ViewModelBase row in grid.Data.Where(row => row.Expanded))
		{
			await grid.ExpandRow(row);
		}
	}

	private void OnCellRender(DataGridCellRenderEventArgs<ViewModelBase> args)
	{
		IDictionary<string, object> attributes = args.Attributes;

		if (args.Data is TestParameterValueCollectionViewModel vm &&
		    ! TestParameterTypeUtils.IsDatasetType(vm.Parameter.Type))
		{
			SetBackgroundColorGrey(args, attributes);
			return;
		}

		if (args.Data is ScalarTestParameterValueViewModel)
		{
			SetBackgroundColorGrey(args, attributes);
		}
	}

	private static void SetBackgroundColorGrey(
		[NotNull] DataGridCellRenderEventArgs<ViewModelBase> args,
		[NotNull] IDictionary<string, object> attributes)
	{
		if (args.Column.Property == "ModelName")
		{
			attributes.Add("colspan", 3);

			if (attributes.ContainsKey("style"))
			{
				attributes["style"] += "; background-color: grey";
			}
			else
			{
				attributes.Add("style", "background-color: grey");
			}
		}
	}

	#endregion

	#region buttons

	private async void DeleteRowClicked()
	{
		Assert.False(_childGrids.Count == 0, "no child grids");

		Assert.NotNull(SelectedCollectionRow);

		ViewModel.DeleteRow(SelectedCollectionRow);

		SelectedCollectionRow = null;

		foreach (RadzenDataGrid<ViewModelBase> grid in _childGrids)
		{
			await grid.Reload();
		}
	}

	private void InsertRowClicked()
	{
		Assert.NotNull(SelectedRow);
		
		ViewModelBase row = ViewModel.InsertRow(SelectedRow.Parameter);

		DataGridUtils.Insert(_childGrids, row);
	}

	private void UpClicked()
	{
		Assert.False(_childGrids.Count == 0, "no child grids");
		Assert.NotNull(SelectedCollectionRow);

		RadzenDataGrid<ViewModelBase> childGrid =
			_childGrids.FirstOrDefault(grid => grid.Data.Contains(SelectedCollectionRow));

		Assert.NotNull(childGrid);

		IEnumerable<ViewModelBase> collectionViewModels =
			Rows.Where(vm => vm is TestParameterValueCollectionViewModel);

		foreach (ViewModelBase vm in collectionViewModels)
		{
			List<ViewModelBase> values = Assert.NotNull(vm.Values);

			int index = values.IndexOf(SelectedCollectionRow);

			if (index == -1)
			{
				// selected row is not in this collection view model
				continue;
			}

			values.RemoveAt(index);

			values.Insert(index - 1, SelectedCollectionRow);

			childGrid.Data = values;
			childGrid.Reload();

			SelectedCollectionRow.NotifyDirty();
		}
	}

	private void DownClicked()
	{
		Assert.False(_childGrids.Count == 0, "no child grids");
		Assert.NotNull(SelectedCollectionRow);

		RadzenDataGrid<ViewModelBase> childGrid =
			_childGrids.FirstOrDefault(grid => grid.Data.Contains(SelectedCollectionRow));

		Assert.NotNull(childGrid);

		IEnumerable<ViewModelBase> collectionViewModels =
			Rows.Where(vm => vm is TestParameterValueCollectionViewModel);

		foreach (ViewModelBase vm in collectionViewModels)
		{
			List<ViewModelBase> values = Assert.NotNull(vm.Values);

			int index = values.IndexOf(SelectedCollectionRow);
			
			if (index == -1)
			{
				// selected row is not in this collection view model
				continue;
			}

			values.RemoveAt(index);

			values.Insert(index + 1, SelectedCollectionRow);

			childGrid.Data = values;
			childGrid.Reload();

			SelectedCollectionRow.NotifyDirty();
		}
	}

	#endregion

	#region button disabled

	private bool ButtonDisabledCore()
	{
		if (Rows.Count == 0)
		{
			return true;
		}

		return false;
	}

	private bool InsertRowButtonDisabled()
	{
		if (ButtonDisabledCore())
		{
			return true;
		}

		if (SelectedRow is not TestParameterValueCollectionViewModel)
		{
			return true;
		}

		return false;
	}

	private bool DeleteRowButtonDisabled()
	{
		if (ButtonDisabledCore())
		{
			return true;
		}

		if (_childGrids.Count == 0)
		{
			return true;
		}

		if (_childGrids.Any(grid => grid.Data.Count() == 0))
		{
			return true;
		}
		
		if (SelectedCollectionRow == null)
		{
			return true;
		}

		return false;
	}

	private bool NavigationButtonDisabledCore()
	{
		if (ButtonDisabledCore())
		{
			return true;
		}

		if (_childGrids.Count == 0)
		{
			return true;
		}

		if (SelectedCollectionRow == null)
		{
			return true;
		}

		return false;
	}

	private bool UpButtonDisabled()
	{
		if (NavigationButtonDisabledCore())
		{
			return true;
		}

		IEnumerable<ViewModelBase> collectionViewModels =
			Rows.Where(vm => vm is TestParameterValueCollectionViewModel);

		return collectionViewModels.Any(vm =>
		{
			int index = vm.Values.IndexOf(SelectedCollectionRow);

			return index == 0;
		});
	}

	private bool DownButtonDisabled()
	{
		if (NavigationButtonDisabledCore())
		{
			return true;
		}

		IEnumerable<ViewModelBase> collectionViewModels =
			Rows.Where(vm => vm is TestParameterValueCollectionViewModel);

		return collectionViewModels.Any(vm =>
		{
			int index = vm.Values.IndexOf(SelectedCollectionRow);

			return index >= vm.Values.Count - 1;
		});
	}

	#endregion

	#region unused

	//protected override void OnInitialized()
	//{
	//	// 1
	//	//ViewModel.SavedChanges += OnSavedChanges;

	//	base.OnInitialized();
	//}

	private void OnSavedChanges(object sender, EventArgs e)
	{
		foreach (ViewModelBase row in Rows)
		{
			_mainGrid.UpdateRow(row);
		}
	}

	#endregion

	private void OnFocusOut(FocusEventArgs args)
	{
	}

	private void OnChildGridRender(DataGridRenderEventArgs<ViewModelBase> args)
	{
		List<RadzenDataGrid<ViewModelBase>> grids = _childGrids;

		if (! grids.Contains(args.Grid))
		{
			_childGrids.Add(args.Grid);
		}
	}
}

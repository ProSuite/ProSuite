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
using ProSuite.DomainModel.Core.QA;
using Radzen;
using Radzen.Blazor;

namespace ProSuite.DdxEditor.Content.Blazor;

public partial class QualityConditionTableViewBlazor : IDisposable
{
	// ReSharper disable once NotNullMemberIsNotInitialized
	[NotNull] private RadzenDataGrid<ViewModelBase> _mainGrid;
	[CanBeNull] private RadzenDataGrid<ViewModelBase> _collectionGrid;

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
	public ViewModelBase SelectedCollectionRow { get; set; }

	public void Dispose()
	{
		_viewModel.PropertyChanged -= OnPropertyChanged;
	}

	private async void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		foreach (ViewModelBase row in Rows)
		{
			await _mainGrid.UpdateRow(row);
		}

		if (_collectionGrid == null)
		{
			return;
		}

		foreach (ViewModelBase row in Rows.OfType<TestParameterValueCollectionViewModel>()
										  .SelectMany(row => row.Values))
		{
			await _collectionGrid.UpdateRow(row);
		}
	}

	private async void OnRowClick(DataGridRowMouseEventArgs<ViewModelBase> args)
	{
		Assert.True(_mainGrid.Data.Contains(args.Data), "row is not from grid");

		await DataGridUtils.UpdateRowIfNotNull(_collectionGrid, SelectedCollectionRow);

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

	private async void OnCollectionRowClick(DataGridRowMouseEventArgs<ViewModelBase> args)
	{
		Assert.NotNull(_collectionGrid);
		Assert.True(_collectionGrid.Data.Contains(args.Data), "row is not from collection grid");
		
		await DataGridUtils.UpdateRowIfNotNull(_mainGrid, SelectedRow);

		ViewModelBase recent = SelectedCollectionRow;

		SelectedCollectionRow = args.Data;
		SelectedRow = null;

		if (! Equals(recent, SelectedCollectionRow))
		{
			await DataGridUtils.UpdateRowIfNotNull(_collectionGrid, recent);

			if (SelectedCollectionRow.New)
			{
				await DataGridUtils.UpdateRow(_collectionGrid, SelectedCollectionRow);
			}

			await DataGridUtils.EditRow(_collectionGrid, SelectedCollectionRow);
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
		//args.Attributes.Add("style", "min-height: 0px; height: 40px");
		args.Attributes.Add("height", "auto");

		if (args.Data is ScalarTestParameterValueViewModel)
		{
			if (args.Column.Property == "ModelName")
			{
				args.Attributes.Add("colspan", 3);
				args.Attributes.Add("style", "background-color: grey;");
			}
		}
	}

	#endregion

	#region buttons

	private async void DeleteRowClicked()
	{
		Assert.NotNull(_collectionGrid);

		Assert.NotNull(SelectedCollectionRow);

		ViewModel.DeleteRow(SelectedCollectionRow);

		SelectedCollectionRow = null;

		await _collectionGrid.Reload();
	}

	private void InsertRowClicked()
	{
		Assert.NotNull(_collectionGrid);

		ViewModelBase first = _collectionGrid.Data.FirstOrDefault();
		Assert.NotNull(first);
		
		ViewModelBase row = ViewModel.InsertRow(first.Parameter);

		_collectionGrid.InsertRow(row);
	}

	private void UpClicked()
	{
		Assert.NotNull(_collectionGrid);
		Assert.NotNull(SelectedCollectionRow);
		
		IEnumerable<ViewModelBase> collectionViewModels =
			Rows.Where(vm => vm is TestParameterValueCollectionViewModel);

		foreach (ViewModelBase vm in collectionViewModels)
		{
			List<ViewModelBase> values = vm.Values;

			int index = values.IndexOf(SelectedCollectionRow);

			if (index == -1)
			{
				// selected row is not in this collection view model
				continue;
			}

			values.RemoveAt(index);

			values.Insert(index - 1, SelectedCollectionRow);

			_collectionGrid.Data = values;
			_collectionGrid.Reload();
		}
	}

	private void DownClicked()
	{
		Assert.NotNull(_collectionGrid);
		Assert.NotNull(SelectedCollectionRow);

		IEnumerable<ViewModelBase> collectionViewModels =
			Rows.Where(vm => vm is TestParameterValueCollectionViewModel);

		foreach (ViewModelBase vm in collectionViewModels)
		{
			List<ViewModelBase> values = vm.Values;
			int index = values.IndexOf(SelectedCollectionRow);

			if (index == -1)
			{
				// selected row is not in this collection view model
				continue;
			}

			values.RemoveAt(index);

			values.Insert(index + 1, SelectedCollectionRow);

			_collectionGrid.Data = values;
			_collectionGrid.Reload();
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

		if (_collectionGrid == null)
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

		if (_collectionGrid == null)
		{
			return true;
		}

		if (Enumerable.Count(_collectionGrid.Data) == 1)
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
}

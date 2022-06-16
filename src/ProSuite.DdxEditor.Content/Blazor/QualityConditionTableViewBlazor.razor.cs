using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;
using Radzen;
using Radzen.Blazor;

namespace ProSuite.DdxEditor.Content.Blazor;

public partial class QualityConditionTableViewBlazor : IDisposable
{
	// ReSharper disable once NotNullMemberIsNotInitialized
	[NotNull] private RadzenDataGrid<ViewModelBase> _mainGrid;
	[CanBeNull] private RadzenDataGrid<ViewModelBase> _collectionGrid;

	// ReSharper disable once NotNullMemberIsNotInitialized
	[NotNull] private QualityConditionViewModel _viewModel;

	[NotNull]
	[Parameter]
	// ReSharper disable once NotNullMemberIsNotInitialized
	public QualityConditionViewModel ViewModel
	{
		get => _viewModel;
		set
		{
			_viewModel = value;

			_viewModel.PropertyChanged += OnPropertyChanged;
		}
	}

	private async void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (_collectionGrid != null)
		{
			foreach (ViewModelBase row in Rows.SelectMany(row => row.Values))
			{
				await _collectionGrid.UpdateRow(row);
			}
		}

		foreach (ViewModelBase row in Rows)
		{
			await _mainGrid.UpdateRow(row);
		}

		StateHasChanged();
	}

	private IList<ViewModelBase> Rows => ViewModel.Rows;

	public ViewModelBase SelectedRow { get; set; }

	//public IList<ViewModelBase> SelectedRows { get; set; }

	public void Dispose()
	{
		_viewModel.PropertyChanged -= OnPropertyChanged;
		//ViewModel.SavedChanges -= OnSavedChanges;
	}

	private async Task EditRow([NotNull] ViewModelBase row)
	{
		if (_collectionGrid != null)
		{
			await _collectionGrid.EditRow(row);
		}

		await _mainGrid.EditRow(row);
	}

	#region layout

	private static void OnRowCreate(ViewModelBase row) { }

	// todo daro implement ViewUtils
	private async void OnRowClick(DataGridRowMouseEventArgs<ViewModelBase> arg)
	{
		ViewModelBase recent = SelectedRow;

		ViewModelBase current = arg.Data;

		if (! Equals(recent, current))
		{
			recent?.StopEditing();
			current.StartEditing();
		}

		SelectedRow = current;

		await EditRow(SelectedRow);
	}

	private static void OnRowRender(RowRenderEventArgs<ViewModelBase> args)
	{
		// todo daro inline
		bool isExpandable = args.Data.Values != null;

		args.Expandable = isExpandable;
	}

	private static void OnRender(DataGridRenderEventArgs<ViewModelBase> args, ViewModelBase parent)
	{
		RadzenDataGrid<ViewModelBase> grid = args.Grid;

		//if (parent is TestParameterValueCollectionViewModel && ! _collectionGrids.ContainsKey("dataset"))
		//{
		//	grid.UniqueID = "dataset";
		//	_collectionGrids.Add("dataset", grid);

		//	return;
		//}

		//if (parent is ScalarTestParameterValueViewModel && ! _collectionGrids.ContainsKey("scalar"))
		//{
		//	grid.UniqueID = "scalar";
		//	_collectionGrids.Add("scalar", grid);
		//}
	}

	private void OnCellRender(DataGridCellRenderEventArgs<ViewModelBase> args)
	{
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

	private void InsertRowClicked()
	{
		Assert.NotNull(_collectionGrid);

		//var renderFragment = _testParametersGrid.RenderFragment;

		//var collectionViewModel = parent as TestParameterValueCollectionViewModel;
		//if (collectionViewModel == null)
		//{
		//	return;
		//}

		//Assert.NotNull(collectionViewModel, "no collection view model");
		//Assert.True(collectionViewModel.Values.Count > 0, "empty collection view model");

		//collectionViewModel.Values.Add(new EmptyTestParameterValueViewModel());

		//if (_collectionGrids.TryGetValue(dataGrid.UniqueID, out RadzenDataGrid<ViewModelBase> grid))
		//{
		//	grid.InsertRow(new EmptyTestParameterValueViewModel());
		//}
	}

	private void UpClicked()
	{
		Assert.NotNull(_collectionGrid);
		Assert.NotNull(SelectedRow);
		
		IEnumerable<ViewModelBase> collectionViewModels =
			Rows.Where(vm => vm is TestParameterValueCollectionViewModel);

		foreach (ViewModelBase vm in collectionViewModels)
		{
			List<ViewModelBase> values = vm.Values;

			int index = values.IndexOf(SelectedRow);

			if (index == -1)
			{
				// selected row is not in this collection view model
				continue;
			}

			values.RemoveAt(index);

			values.Insert(index - 1, SelectedRow);

			_collectionGrid.Data = values;
			_collectionGrid.Reload();
		}
	}

	private void DownClicked()
	{
		Assert.NotNull(_collectionGrid);
		Assert.NotNull(SelectedRow);

		
		IEnumerable<ViewModelBase> collectionViewModels =
			Rows.Where(vm => vm is TestParameterValueCollectionViewModel);

		foreach (ViewModelBase vm in collectionViewModels)
		{
			List<ViewModelBase> values = vm.Values;
			int index = values.IndexOf(SelectedRow);

			if (index == -1)
			{
				// selected row is not in this collection view model
				continue;
			}

			values.RemoveAt(index);

			values.Insert(index + 1, SelectedRow);

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

		if (SelectedRow is not TestParameterValueCollectionViewModel collectionViewModel)
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

		if (SelectedRow == null)
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
			int index = vm.Values.IndexOf(SelectedRow);

			return index == 0 || index == -1;
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
			int index = vm.Values.IndexOf(SelectedRow);

			return index == vm.Values.Count - 1 || index == -1;
		});
	}

	#endregion

	#region unused

	protected override void OnInitialized()
	{
		// 1
		//ViewModel.SavedChanges += OnSavedChanges;

		base.OnInitialized();
	}

	private void OnSavedChanges(object sender, EventArgs e)
	{
		foreach (ViewModelBase row in Rows)
		{
			_mainGrid.UpdateRow(row);
		}
	}

	#endregion
}

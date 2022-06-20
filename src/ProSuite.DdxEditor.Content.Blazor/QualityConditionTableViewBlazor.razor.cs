using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Misc;
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

	private Latch _latch = new Latch();

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

	private IList<ViewModelBase> Rows => ViewModel.Rows;

	public ViewModelBase SelectedRow { get; set; }

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

	private async void OnRowClick(DataGridRowMouseEventArgs<ViewModelBase> arg)
	{
		ViewModelBase recent = SelectedRow;

		SelectedRow = arg.Data;

		if (SelectedRow is TestParameterValueCollectionViewModel)
		{
			recent?.StopEditing();
			return;
		}

		if (_collectionGrid != null)
		{
			Assert.True(SelectedRow is not TestParameterValueCollectionViewModel,
			            $"{nameof(TestParameterValueCollectionViewModel)} cannot be edited");

			// start editing selected row
			if (_collectionGrid.Data.Contains(SelectedRow))
			{
				if (! Equals(recent, SelectedRow))
				{
					if (recent != null)
					{
						recent.StopEditing();
						await _collectionGrid.UpdateRow(recent);
					}

					SelectedRow.StartEditing();
					await _collectionGrid.UpdateRow(SelectedRow);
					await _collectionGrid.EditRow(SelectedRow);

					return;
				}
			}
		}

		if (_mainGrid.Data.Contains(SelectedRow))
		{
			if (! Equals(recent, SelectedRow))
			{
				recent?.StopEditing();
				await _mainGrid.UpdateRow(recent);

				SelectedRow.StartEditing();
				await _mainGrid.EditRow(SelectedRow);
			}

			await _mainGrid.UpdateRow(SelectedRow);
		}
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

		Assert.NotNull(SelectedRow);

		ViewModel.DeleteRow(SelectedRow);

		SelectedRow = null;

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

		if (SelectedRow is TestParameterValueCollectionViewModel)
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

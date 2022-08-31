using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Prism.Events;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor.Events;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;
using Radzen;
using Radzen.Blazor;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public abstract class DataGridBlazorBase : ComponentBase, IDisposable
{
	[NotNull] private IList<ViewModelBase> _selectedRows = new List<ViewModelBase>();

	// ReSharper disable once NotNullMemberIsNotInitialized
	[NotNull] protected RadzenDataGrid<ViewModelBase> DataGrid;

	[NotNull]
	[Inject]
	// ReSharper disable once NotNullMemberIsNotInitialized
	public IEventAggregator EventAggregator { get; set; }

	[CanBeNull]
	public ViewModelBase SelectedRow { get; set; }

	[NotNull]
	public IList<ViewModelBase> SelectedRows
	{
		get => _selectedRows;
		set
		{
			ViewModelBase row = value.LastOrDefault();

			// prevent dummy row from being selected
			if (row is DummyTestParameterValueViewModel)
			{
				return;
			}

			_selectedRows = value;

			SelectedRow = row;
		}
	}

	protected IEnumerable<ViewModelBase> Rows { get; set; }

	public async void Dispose()
	{
		DisposeCore();

		EventAggregator.GetEvent<SelectedRowChangedEvent>().Unsubscribe(OnSelectedRowChangedAsync);

		await DataGrid.UpdateIfNotNull(SelectedRow);
	}

	protected virtual void DisposeCore(){}

	protected override void OnInitialized()
	{
		OnInitializedCore();
	}

	protected virtual void OnInitializedCore()
	{
		EventAggregator.GetEvent<SelectedRowChangedEvent>().Subscribe(OnSelectedRowChangedAsync);
	}

	protected async void OnSelectedRowChangedAsync([NotNull] ViewModelBase current)
	{
		Assert.ArgumentNotNull(current, nameof(current));

		ViewModelBase recent = SelectedRow;

		if (DataGrid.Contains(current))
		{
			if (Equals(recent, current))
			{
				return;
			}

			await DataGrid.UpdateIfNotNull(recent);

			await DataGrid.Edit(current);
		}
		else
		{
			await DataGrid.UpdateIfNotNull(recent);

			SelectedRows = new List<ViewModelBase>();
		}

		StateHasChanged();
	}

	protected void OnRowClick(DataGridRowMouseEventArgs<ViewModelBase> args)
	{
		OnRowClickCore(args.Data);
	}

	protected virtual void OnRowClickCore(ViewModelBase row)
	{
		Assert.False(row is DummyTestParameterValueViewModel,
		             $"unexpected type {nameof(DummyTestParameterValueViewModel)}");

		EventAggregator.GetEvent<SelectedRowChangedEvent>().Publish(row);
	}
}

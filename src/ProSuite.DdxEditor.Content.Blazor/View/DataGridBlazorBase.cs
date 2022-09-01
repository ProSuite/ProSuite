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
	// ReSharper disable once NotNullMemberIsNotInitialized
	[NotNull] protected RadzenDataGrid<ViewModelBase> DataGrid;

	[NotNull]
	[Inject]
	// ReSharper disable once NotNullMemberIsNotInitialized
	public IEventAggregator EventAggregator { get; set; }

	[CanBeNull]
	public ViewModelBase SelectedRow => SelectedRows.LastOrDefault(row => row is not DummyTestParameterValueViewModel);

	[NotNull]
	public IList<ViewModelBase> SelectedRows { get; set; } = new List<ViewModelBase>();

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

	private async void OnSelectedRowChangedAsync([NotNull] ViewModelBase current)
	{
		Assert.ArgumentNotNull(current, nameof(current));

		if (! DataGrid.Contains(current))
		{
			await DataGrid.SelectRow(null);
		}
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

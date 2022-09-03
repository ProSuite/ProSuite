using System;
using Microsoft.AspNetCore.Components;
using Prism.Events;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor.Events;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public abstract class TestParameterValueBlazorBase<T> : ComponentBase, IDisposable
{
	[Parameter]
	public ViewModelBase ViewModel { get; set; }
	
	[NotNull]
	[Inject]
	// ReSharper disable once NotNullMemberIsNotInitialized
	public IEventAggregator EventAggregator { get; set; }

	public bool Selected { get; set; }

	protected string ErrorMessage => ViewModel.ErrorMessage;
	
	protected override void OnInitialized()
	{
		OnInitializedCore();
	}

	protected virtual void OnInitializedCore()
	{
		EventAggregator.GetEvent<SelectedRowChangedEvent>().Subscribe(OnSelectedRowChanged);
	}

	private void OnSelectedRowChanged([NotNull] ViewModelBase current)
	{
		Selected = ViewModel.Equals(current);
	}

	public void Dispose()
	{
		EventAggregator.GetEvent<SelectedRowChangedEvent>().Unsubscribe(OnSelectedRowChanged);
		ViewModel?.Dispose();
	}

	protected void SetValue([CanBeNull] object value)
	{
		ViewModel.Value = value;
	}

	protected T GetValue()
	{
		return (T) ViewModel.Value;
	}
}

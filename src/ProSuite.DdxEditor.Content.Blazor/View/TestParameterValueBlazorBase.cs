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
		// Dispose() in any view (blazor) should not dispose its view model.
		// All view models are centrally stored in InstanceConfigurationViewModel.Values.
		// Only InstanceConfigurationViewModel.Dispose() should dispose the view models.
		// InstanceConfigurationViewModel.Dispose() is called when the InstanceConfigurationItem (or QualityConditionItem) changes.

		// TestParameterValueBlazorBase.Dispose() is called whenever a blazor view gets distroyed, e.g. close a nested grid.
		// Disposing its view model in this situation unregisters the view model from Observer.OnRowPropertyChanged event.
		EventAggregator.GetEvent<SelectedRowChangedEvent>().Unsubscribe(OnSelectedRowChanged);
	}

	protected void SetValue([CanBeNull] object value)
	{
		ViewModel.Value = value;
	}

	protected T GetValue()
	{
		return (T) ViewModel.Value;
	}

	protected void OnClickResetValue()
	{
		ViewModel.ResetValue();
	}
}

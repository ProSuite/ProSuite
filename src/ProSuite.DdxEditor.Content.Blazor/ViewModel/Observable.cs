using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Prism.Events;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;
using ProSuite.DdxEditor.Content.Blazor.Events;
using ProSuite.Shared.IoCRoot;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public abstract class Observable : IDisposable, INotifyPropertyChanged
{
	[CanBeNull] private readonly string _customErrorMessage;
	[NotNull] private readonly string _defaultErrorMessage = "Value not set";
	private readonly bool _required;

	[CanBeNull] private SubscriptionToken _eventToken;

	protected Observable([NotNull] IInstanceConfigurationViewModel observer,
	                     [CanBeNull] string customErrorMessage,
	                     bool required = false)
	{
		Assert.ArgumentNotNull(observer, nameof(observer));

		_required = required;
		_customErrorMessage = customErrorMessage;

		Observer = observer;

		PropertyChanged += Observer.OnRowPropertyChanged;
	}

	[NotNull]
	protected IInstanceConfigurationViewModel Observer { get; }

	public string ErrorMessage { get; private set; }

	public void Dispose()
	{
		PropertyChanged -= Observer.OnRowPropertyChanged;

		UnwireEvent();

		DisposeCore();
	}

	protected virtual void DisposeCore() { }

	public event PropertyChangedEventHandler PropertyChanged;

	protected bool SetProperty<T>(ref T backingField, T value,
	                              [CallerMemberName] string propertyName = null)
	{
		if (Equals(backingField, value))
		{
			return false;
		}

		backingField = value;

		OnPropertyChanged(propertyName);

		return true;
	}

	protected bool Validate()
	{
		if (! _required)
		{
			ErrorMessage = null;
			return true;
		}

		if (ValidateCore())
		{
			ErrorMessage = null;
			return true;
		}

		string errorMessage = ! string.IsNullOrEmpty(_customErrorMessage)
			                      ? _customErrorMessage
			                      : _defaultErrorMessage;

		ErrorMessage = errorMessage;

		return false;
	}

	protected abstract bool ValidateCore();

	protected virtual void RegisterMessageCore(Notification notification, string message) { }

	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		NotifyDirty();

		Validate();

		if (! _required)
		{
			WireEvent();
		}

		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private void OnValidateForPersistence(Notification notification)
	{
		Assert.True(_required, "Validation event should not fire");

		if (Validate())
		{
			return;
		}

		// message should be not null if it is not valid
		string message = Assert.NotNullOrEmpty(ErrorMessage);

		RegisterMessageCore(notification, message);
	}

	private void WireEvent()
	{
		if (_eventToken != null)
		{
			return;
		}

		var eventAggregator =
			ContainerRegistry.Current.Resolve<IEventAggregator>();

		_eventToken = eventAggregator.GetEvent<ValidateForPersistenceEvent>()
		                             .Subscribe(OnValidateForPersistence);
	}

	private void UnwireEvent()
	{
		var eventAggregator =
			ContainerRegistry.Current.Resolve<IEventAggregator>();

		eventAggregator.GetEvent<ValidateForPersistenceEvent>().Unsubscribe(_eventToken);

		_eventToken = null;
	}

	private void NotifyDirty()
	{
		Observer.NotifyChanged(true);
	}
}

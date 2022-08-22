using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Prism.Events;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;
using ProSuite.DdxEditor.Framework.Events;
using ProSuite.Shared.IoCRoot;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public abstract class Observable : IDisposable, INotifyPropertyChanged
{
	private readonly bool _required;
	[CanBeNull] private SubscriptionToken _eventToken;

	protected Observable([NotNull] IInstanceConfigurationViewModel observer, bool required = false)
	{
		Assert.ArgumentNotNull(observer, nameof(observer));

		_required = required;

		Observer = observer;

		PropertyChanged += Observer.OnRowPropertyChanged;
	}

	// todo daro rename?
	[NotNull]
	protected IInstanceConfigurationViewModel Observer { get; }

	public string ErrorMessage { get; set; }

	[NotNull]
	public Func<bool> Validation { get; set; }

	public void Dispose()
	{
		PropertyChanged -= Observer.OnRowPropertyChanged;

		UnwireEvent();

		DisposeCore();
	}

	public event PropertyChangedEventHandler PropertyChanged;

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
		if (_eventToken == null)
		{
			return;
		}

		var eventAggregator =
			ContainerRegistry.Current.Resolve<IEventAggregator>();

		eventAggregator.GetEvent<ValidateForPersistenceEvent>().Unsubscribe(_eventToken);

		_eventToken = null;
	}

	private void NotifyDirty()
	{
		Observer.NotifyChanged(true);

		if (_required)
		{
			WireEvent();
		}
	}

	private void OnValidateForPersistence(Notification notification)
	{
		if (Validate())
		{
			return;
		}

		// message should be not null if it is not valid
		string message = Assert.NotNullOrEmpty(ErrorMessage);

		RegisterMessageCore(notification, message);
	}

	protected virtual void DisposeCore() { }

	protected virtual void RegisterMessageCore(Notification notification, string message) { }

	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		NotifyDirty();

		Validate();

		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	protected virtual string GetErrorMessageCore()
	{
		return null;
	}

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
		bool result = Validation.Invoke();

		ErrorMessage = result ? null : GetErrorMessageCore();

		return result;
	}
}

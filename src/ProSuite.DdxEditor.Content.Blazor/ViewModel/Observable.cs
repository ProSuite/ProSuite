using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public abstract class Observable : IDisposable, INotifyPropertyChanged
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[CanBeNull] private readonly string _customErrorMessage;
	[NotNull] private readonly string _defaultErrorMessage = "Value not set";
	private readonly bool _required;
	
	protected Observable([NotNull] IInstanceConfigurationViewModel observer,
	                     [CanBeNull] string customErrorMessage,
	                     bool required = false)
	{
		Assert.ArgumentNotNull(observer, nameof(observer));

		// todo daro: use RequiredAttribute on Transformer or QaTest
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

	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		// todo daro should validation prevent from being able to save?
		// Then do not notify dirty.
		Validate();

		NotifyDirty();

		_msg.VerboseDebug(() => $"OnRowPropertyChanged: {propertyName}, {this}");

		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private void NotifyDirty()
	{
		Observer.NotifyChanged(true);
	}
}

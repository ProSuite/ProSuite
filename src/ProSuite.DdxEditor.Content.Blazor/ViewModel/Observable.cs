using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

// substitute with Prism
public abstract class Observable : IDisposable, INotifyPropertyChanged
{
	protected Observable([NotNull] IInstanceConfigurationViewModel observer)
	{
		Assert.ArgumentNotNull(observer, nameof(observer));

		Observer = observer;
	}

	// todo daro rename?
	[NotNull]
	protected IInstanceConfigurationViewModel Observer { get; }

	public void Dispose() { }

	public event PropertyChangedEventHandler PropertyChanged;

	public void NotifyDirty()
	{
		Observer.NotifyChanged(true);
	}

	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		NotifyDirty();

		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
}

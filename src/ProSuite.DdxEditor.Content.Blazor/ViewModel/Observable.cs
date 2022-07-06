using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

// substitute with Prism
public abstract class Observable : IDisposable, INotifyPropertyChanged
{
	[CanBeNull] private readonly IViewModel _observer;

	protected Observable() { }

	protected Observable([CanBeNull] IViewModel observer)
	{
		_observer = observer;
	}

	public bool Dirty { get; set; }

	public bool New { get; set; }

	public void Dispose() { }

	public event PropertyChangedEventHandler PropertyChanged;

	protected virtual void OnSavedChangesCore() { }

	public void NotifyDirty()
	{
		Dirty = true;

		_observer?.NotifyChanged(true);
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

		NotifyDirty();

		OnPropertyChanged(propertyName);

		return true;
	}
}

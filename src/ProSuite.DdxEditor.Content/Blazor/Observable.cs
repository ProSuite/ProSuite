using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;

namespace ProSuite.DdxEditor.Content.Blazor;

// substitute with Prism
public abstract class Observable : IDisposable, INotifyPropertyChanged
{
	[CanBeNull] private readonly IViewModel _observer;

	protected Observable([CanBeNull] IViewModel observer)
	{
		_observer = observer;

		//_observer.SavedChanges += OnSavedChanges;
	}

	public bool Dirty { get; set; }

	public void Dispose()
	{
		//_observer.SavedChanges -= OnSavedChanges;
	}

	public event PropertyChangedEventHandler PropertyChanged;

	private void OnSavedChanges(object sender, EventArgs e)
	{
		Dirty = false;

		OnSavedChangesCore();
	}

	protected virtual void OnSavedChangesCore() { }

	private void NotifyDirty()
	{
		Dirty = true;

		_observer?.NotifyChanged(true);
	}

	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
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

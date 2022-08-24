using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public interface IDataGridViewModel : INotifyPropertyChanged, IDisposable
{
	IList<ViewModelBase> Values { get; }
}

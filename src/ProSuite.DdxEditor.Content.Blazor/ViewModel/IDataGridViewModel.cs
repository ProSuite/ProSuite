using System.Collections.Generic;
using System.ComponentModel;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public interface IDataGridViewModel : INotifyPropertyChanged
{
	IList<ViewModelBase> Values { get; }

	void WireEvents(ViewModelBase row);

	void UnwireEvents(ViewModelBase row);
}

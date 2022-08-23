using System;
using System.Collections.Generic;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public interface IDataGridViewModel : IDisposable
{
	IList<ViewModelBase> Values { get; }
}

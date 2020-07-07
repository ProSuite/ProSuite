using System;

namespace ProSuite.Commons.UI.PropertyEditors
{
	public interface IDataChanged
	{
		event EventHandler DataChanged;

		void OnDataChanged(EventArgs args);
	}
}

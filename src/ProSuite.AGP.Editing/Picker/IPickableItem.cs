using System;
using System.ComponentModel;
using ArcGIS.Core.Geometry;

namespace ProSuite.AGP.Editing.Picker
{
	public interface IPickableItem : INotifyPropertyChanged
	{
		string ItemText { get; }

		bool IsSelected { get; set; }

		Geometry Geometry { get; set; }

		Uri ItemImageUri { get; set; }
	}
}

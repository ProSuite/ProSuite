using ArcGIS.Core.Geometry;
using System;
using System.ComponentModel;

namespace ProSuite.AGP.Picker

{
	public interface IPickableItem: INotifyPropertyChanged
	{
		string ItemText { get;}

		bool IsSelected { get; set; }
		
		Geometry Geometry { get; set; }

		Uri ItemImageUri { get; set; }
		
	}
}

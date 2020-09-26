using System;
using System.ComponentModel;
using System.Windows.Media;
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace ProSuite.AGP.Picker

{
	public interface IPickableItem: INotifyPropertyChanged
	{
		string ItemText { get;}

		bool IsSelected { get; set; }
		
		Geometry Geometry { get; set; }

		ImageSource ItemImageSource { get; }
		
	}
}

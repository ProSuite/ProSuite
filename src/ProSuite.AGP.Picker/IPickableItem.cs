using ArcGIS.Core.Geometry;
using System;

namespace ProSuite.AGP.Picker

{
	public interface IPickableItem
	{
		string ItemText { get;}

		bool IsSelected { get; set; }
		
		Geometry Geometry { get; set; }

		Uri ItemImageUri { get; set; }
		
	}
}

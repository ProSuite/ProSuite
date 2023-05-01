using System.ComponentModel;
using System.Windows.Media;
using ProSuite.AGP.Editing.Picker;
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace ProSuite.AGP.Editing.Test.Picker
{
	class PickableFeatureMock : IPickableItem
	{
		public event PropertyChangedEventHandler PropertyChanged;
		public string ItemText { get; }
		public bool IsSelected { get; set; }
		public Geometry Geometry { get; set; }
		public ImageSource ItemImageSource { get; }
		public double Score { get; set; }
		public bool Disjoint { get; set; }
	}
}

using System.ComponentModel;
using System.Windows.Media;
using ProSuite.AGP.Editing.Picker;
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace ProSuite.AGP.Editing.Test.Picker
{
	class PickableFeatureMock : IPickableItem
	{
		public event PropertyChangedEventHandler PropertyChanged;
		public string DisplayValue { get; }
		public bool Selected { get; set; }
		public Geometry Geometry { get; set; }
		public ImageSource ImageSource { get; }
		public double Score { get; set; }
		public int ShapeDimension { get; }
		public bool Disjoint { get; set; }
	}
}

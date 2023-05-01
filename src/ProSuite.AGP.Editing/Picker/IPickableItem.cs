using System.ComponentModel;
using System.Windows.Media;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace ProSuite.AGP.Editing.Picker
{
	public interface IPickableItem : INotifyPropertyChanged
	{
		string ItemText { get; }

		// todo daro rename Selected
		bool IsSelected { get; set; }

		[CanBeNull]
		Geometry Geometry { get; set; }

		ImageSource ItemImageSource { get; }

		double Score { get; set; }
	}
}

using System.Windows;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.Picker
{
	public class PickerPrecedence : PickerPrecedenceBase
	{
		[UsedImplicitly]
		public PickerPrecedence(Geometry selectionGeometry,
		                        int selectionTolerance,
		                        Point pickerLocation) : base(
			selectionGeometry, selectionTolerance, pickerLocation) { }
	}
}

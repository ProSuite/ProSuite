using System.Windows;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Picker
{
	public class PickerPrecedence : PickerPrecedenceBase
	{
		[UsedImplicitly]
		public PickerPrecedence([NotNull] Geometry sketchGeometry,
		                         int selectionTolerance,
		                         Point pickerLocation) : base(
			sketchGeometry, selectionTolerance, pickerLocation) { }
	}
}

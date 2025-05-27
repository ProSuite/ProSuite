using System.Windows;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Picker
{
	public class PickerPrecedence : PickerPrecedenceBase
	{
		[UsedImplicitly]
		public PickerPrecedence([NotNull] Geometry sketchGeometry,
		                         int tolerance,
		                         Point pickerLocation) : base(
			sketchGeometry, tolerance, pickerLocation) { }
	}
}

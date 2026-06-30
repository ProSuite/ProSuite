using System.Windows;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Picker;

public class PickerPrecedence : PickerPrecedenceBase
{
	/// <summary>
	/// Initializes a new instance of the <see cref="PickerPrecedence"/> class.
	/// </summary>
	/// <param name="sketchGeometry">The sketch geometry.</param>
	/// <param name="selectionTolerancePixels">The selection tolerance in pixels.</param>
	/// <param name="pickerLocation">The location of the picker.</param>
	/// <param name="mapView">The map view. Used for coordinate transformations. Must be passed do not change to MapView.Active inside the method.</param>
	[UsedImplicitly]
	public PickerPrecedence([NotNull] Geometry sketchGeometry,
	                        int selectionTolerancePixels,
	                        Point pickerLocation, [NotNull] MapView mapView) : base(
		sketchGeometry, selectionTolerancePixels, pickerLocation, mapView) { }
}

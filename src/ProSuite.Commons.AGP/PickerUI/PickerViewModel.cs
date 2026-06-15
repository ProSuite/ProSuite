using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Picker;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.PickerUI;

public class PickerViewModel : PickerViewModelBase<IPickableItem>
{
	private readonly bool _flashAllMapViews;

	public PickerViewModel([NotNull] Geometry selectionGeometry,
	                       bool flashAllMapViews = false) : base(selectionGeometry)
	{
		_flashAllMapViews = flashAllMapViews;
	}

	[NotNull]
	protected override FlashService CreateFlashService()
	{
		return new FlashService(_flashAllMapViews);
	}
}

using ArcGIS.Core.Geometry;
using ProSuite.AGP.Editing.Picker;

namespace ProSuite.AGP.Editing.PickerUI
{
	public class PickerViewModel : PickerViewModelBase<IPickableItem>
	{
		public PickerViewModel(Geometry selectionGeometry) : base(selectionGeometry) { }

		protected override FlashService CreateFlashService()
		{
			return new FlashService();
		}
	}
}

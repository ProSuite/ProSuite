using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Picker;

namespace ProSuite.Commons.AGP.PickerUI
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

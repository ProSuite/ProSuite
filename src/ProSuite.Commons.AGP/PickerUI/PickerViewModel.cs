using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Picker;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.PickerUI
{
	public class PickerViewModel : PickerViewModelBase<IPickableItem>
	{
		public PickerViewModel([NotNull] Geometry selectionGeometry) : base(selectionGeometry) { }

		[NotNull]
		protected override FlashService CreateFlashService()
		{
			return new FlashService();
		}
	}
}

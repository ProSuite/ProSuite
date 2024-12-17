using ArcGIS.Core.Geometry;
using ProSuite.AGP.Editing.Picker;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.PickerUI
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

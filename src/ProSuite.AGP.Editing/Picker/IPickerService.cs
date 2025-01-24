using System.Collections.Generic;
using System.Threading.Tasks;
using ProSuite.AGP.Editing.PickerUI;

namespace ProSuite.AGP.Editing.Picker
{
	public interface IPickerService
	{
		Task<IPickableItem> Pick(List<IPickableItem> items, IPickerViewModel viewModel);
	}
}

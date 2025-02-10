using System.Collections.Generic;
using System.Threading.Tasks;
using ProSuite.Commons.AGP.PickerUI;

namespace ProSuite.Commons.AGP.Picker
{
	public interface IPickerService
	{
		Task<IPickableItem> Pick(List<IPickableItem> items, IPickerViewModel viewModel);
	}
}

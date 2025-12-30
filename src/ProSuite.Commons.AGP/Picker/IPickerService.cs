using System.Collections.Generic;
using System.Threading.Tasks;
using ProSuite.Commons.AGP.PickerUI;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Picker;

public interface IPickerService
{
	Task<IPickableItem> Pick([NotNull] IEnumerable<IPickableItem> items,
	                         [NotNull] IPickerViewModel viewModel);
}

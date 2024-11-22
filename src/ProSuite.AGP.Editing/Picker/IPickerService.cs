using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProSuite.AGP.Editing.Picker
{
	public interface IPickerService
	{
		Task<T> Pick<T>(List<IPickableItem> items,
		                IPickerPrecedence precedence)
			where T : class, IPickableItem;

		Task<T> PickSingle<T>(IEnumerable<IPickableItem> items,
		                      IPickerPrecedence precedence)
			where T : class, IPickableItem;
	}
}

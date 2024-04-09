using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace ProSuite.AGP.Editing.Picker
{
	public interface IPickerService
	{
		Func<Task<T>> Pick<T>(List<IPickableItem> items,
		                      Point pickerLocation,
		                      IPickerPrecedence precedence)
			where T : class, IPickableItem;

		Func<Task<T>> PickSingle<T>(IEnumerable<IPickableItem> items,
		                            Point pickerLocation,
		                            IPickerPrecedence precedence)
			where T : class, IPickableItem;
	}
}

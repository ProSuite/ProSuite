using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using ProSuite.AGP.Editing.OneClick;

namespace ProSuite.AGP.Editing.Picker
{
	public interface IPickerService
	{
		Func<Task<T>> PickSingle<T>(IEnumerable<IPickableItem> items,
		                            IPickerPrecedence precedence,
		                            IToolMouseEventsAware mouseEvents,
		                            Point pickerLocation)
			where T : class, IPickableItem;
	}
}

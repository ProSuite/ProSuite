using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ProSuite.AGP.Editing.Picker;

namespace ProSuite.AGP.Editing.Test.Picker
{
	public class PickerServiceMock : IPickerService
	{
		public Func<Task<T>> PickSingle<T>(IEnumerable<IPickableItem> items,
		                                   Point pickerLocation,
		                                   IPickerPrecedence precedence)
			where T : class, IPickableItem
		{
			// todo daro remove toList()
			IEnumerable<IPickableItem> orderedItems = precedence.Order(items).ToList();

			IPickableItem bestPick = precedence.PickBest(orderedItems);

			return async () =>
			{
				await Task.FromResult(typeof(T));

				return (T) bestPick;
			};
		}
	}
}

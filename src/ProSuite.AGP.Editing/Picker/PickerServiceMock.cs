using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ProSuite.AGP.Editing.Picker
{
	public class PickerServiceMock
	{
		public async Task<IPickableItem> PickSingleAsync(IEnumerable<IPickableItem> items,
		                                                 Point pickerLocation,
		                                                 IPickerPrecedence precedence)
		{
			var result = new TaskCompletionSource<IPickableItem>();

			IPickableItem item = precedence.Order(items).FirstOrDefault();

			result.SetResult(item);

			return await result.Task;
		}

		public async Task<T> PickSingleAsync<T>(IEnumerable<IPickableItem> items,
		                                        Point pickerLocation,
		                                        IPickerPrecedence precedence)
			where T : IPickableItem
		{
			var result = new TaskCompletionSource<IPickableItem>();

			IPickableItem item = precedence.Order(items).FirstOrDefault();

			result.SetResult(item);

			return (T) await result.Task;
		}
	}
}

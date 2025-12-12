using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProSuite.Commons.AGP.Picker;
using ProSuite.Commons.AGP.PickerUI;

namespace ProSuite.AGP.Editing.Test.Picker
{
	public class PickerServiceMock : IPickerService
	{
		public Task<IPickableItem> Pick(IEnumerable<IPickableItem> items,
		                                IPickerViewModel viewModel)
		{
			throw new NotImplementedException();
		}
	}
}

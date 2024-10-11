using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProSuite.AGP.Editing.Picker;
using ProSuite.AGP.Editing.PickerUI;

namespace ProSuite.AGP.Editing.Test.Picker
{
	public class PickerServiceMock : IPickerService
	{
		public Task<IPickableItem> Pick(List<IPickableItem> items, IPickerViewModel viewModel)
		{
			throw new NotImplementedException();
		}
	}
}

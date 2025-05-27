using System;

namespace ProSuite.Commons.AGP.Picker
{
	[Flags]
	public enum PickerMode
	{
		None = 0,
		PickBest = 1,
		ShowPicker = 2,
		PickAll = 4
	}
}

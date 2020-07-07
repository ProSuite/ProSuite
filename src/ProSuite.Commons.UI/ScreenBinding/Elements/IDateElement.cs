using System;

namespace ProSuite.Commons.UI.ScreenBinding.Elements
{
	public interface IDateElement : IScreenElement
	{
		void EnterDate(DateTime date);

		void SetDateToNull();

		DateTime? GetValue();
	}
}

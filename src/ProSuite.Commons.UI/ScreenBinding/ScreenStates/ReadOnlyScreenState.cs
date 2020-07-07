using System;

namespace ProSuite.Commons.UI.ScreenBinding.ScreenStates
{
	public class ReadOnlyScreenState : IScreenState
	{
		#region IScreenState Members

		public bool IsControlEnabled(object control)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}

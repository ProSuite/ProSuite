namespace ProSuite.Commons.UI.ScreenBinding.ScreenStates
{
	public class NulloScreenState : IScreenState
	{
		#region IScreenState Members

		public bool IsControlEnabled(object control)
		{
			return true;
		}

		#endregion
	}
}

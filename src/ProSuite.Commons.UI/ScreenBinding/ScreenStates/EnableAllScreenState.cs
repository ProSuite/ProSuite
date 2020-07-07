namespace ProSuite.Commons.UI.ScreenBinding.ScreenStates
{
	public class EnableAllScreenState : IScreenState
	{
		#region IScreenState Members

		public bool IsControlEnabled(object control)
		{
			return true;
		}

		#endregion
	}
}

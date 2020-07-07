namespace ProSuite.Commons.UI.ScreenBinding.ScreenStates
{
	public class DisableAllScreenState : IScreenState
	{
		public bool IsControlEnabled(object control)
		{
			return false;
		}
	}
}

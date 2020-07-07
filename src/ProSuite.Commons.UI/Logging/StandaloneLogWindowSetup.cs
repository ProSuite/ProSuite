namespace ProSuite.Commons.UI.Logging
{
	public static class StandaloneLogWindowSetup
	{
		public static void SetLogWindow(ILogWindow logWindow)
		{
			StandaloneLogWindowAppender.SetLogWindow(logWindow);
		}
	}
}

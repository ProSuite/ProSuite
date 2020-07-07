using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.Logging
{
	[UsedImplicitly]
	public class StandaloneLogWindowAppender : LogWindowAppenderBase
	{
		private static ILogWindow _logWindow;

		/// <summary>
		/// Initializes a new instance of the <see cref="StandaloneLogWindowAppender"/> class.
		/// </summary>
		/// <remarks>Empty default constructor</remarks>
		public StandaloneLogWindowAppender() : base("StandaloneLogWindowAppender") { }

		public static void SetLogWindow(ILogWindow logWindow)
		{
			_logWindow = logWindow;
		}

		protected override ILogWindow GetLogWindow()
		{
			return _logWindow;
		}
	}
}

using System.Drawing;

namespace ProSuite.Commons.UI.Logging
{
	public static class LogWindowColors
	{
		private static Color _warning = SystemColors.Info;
		private static Color _error = Color.Coral;
		private static Color _fatal = Color.Red;

		public static Color Warning
		{
			get { return _warning; }
			set { _warning = value; }
		}

		public static Color Error
		{
			get { return _error; }
			set { _error = value; }
		}

		public static Color Fatal
		{
			get { return _fatal; }
			set { _fatal = value; }
		}

		// Dark mode tints the row background instead of replacing it, so the light default
		// text stays readable. SystemColors.Info can't be the tint: it is dark in dark mode.
		internal static Color DarkWarning => Color.Gold;
		internal const float DarkWarningOpacity = 0.25f;

		internal static Color DarkError => Color.Coral;
		internal const float DarkErrorOpacity = 0.35f;

		internal static Color DarkFatal => Color.Red;
		internal const float DarkFatalOpacity = 0.45f;
	}
}

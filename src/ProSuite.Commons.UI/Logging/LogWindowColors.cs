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
	}
}

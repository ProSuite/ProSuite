using System.Runtime.InteropServices;
using System.Windows;

namespace ProSuite.Commons.UI.Input
{
	public static class MouseUtils
	{
		/// <summary>
		/// Returns the current mouse position in (global) screen coordinates.
		/// </summary>
		/// <returns></returns>
		public static Point GetMouseScreenPosition()
		{
			POINT result;
			GetCursorPos(out result);

			return new Point(result.X, result.Y);
		}

		#region Win32 methods

		// ReSharper disable InconsistentNaming, UnassignedField.Global
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

		[DllImport("user32.dll")]
		private static extern bool GetCursorPos(out POINT pt);

		/// <summary>
		/// cursor position
		/// </summary>
		private struct POINT
		{
			/// <summary>
			/// cursor X
			/// </summary>
			public int X;

			/// <summary>
			/// cursor Y
			/// </summary>
			public int Y;
		}

		#endregion
	}
}

using System;
using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.ScreenBinding.Drivers
{
	public interface IControlDriver
	{
		Color BackColor { get; set; }

		bool Visible { get; set; }

		bool Enabled { get; set; }

		void Focus();

		string GetKey();

		void AddLostFocusHandler([NotNull] Action action);

		void RemoveLostFocusHandler([NotNull] Action action);

		void Click();

		void RaiseLostFocus();

		bool IsFocused { get; }

		[NotNull]
		object Control { get; }

		string Text { get; }

		string ToolTipText { get; set; }
	}
}

using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public interface IGroupedListViewItem
	{
		bool Checked { get; }

		bool Enabled { get; }

		[NotNull]
		string Name { get; }

		string Status { get; }

		Color Color { get; }

		[CanBeNull]
		string Group { get; }

		[CanBeNull]
		string ImageKey { get; }

		string ToolTip { get; }
	}
}

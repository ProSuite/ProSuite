using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	/// <summary>
	/// Provides a <see cref="ToolStripComboBox"/> that automatically
	/// stretches to fill all available space on the parent tool strip.
	/// </summary>
	/// <remarks>
	/// The code is taken almost literally from a Microsoft example
	/// at http://msdn.microsoft.com/en-us/library/ms404304.aspx
	/// and works by overriding the GetPreferredSize() method.
	///</remarks>
	public class ToolStripStretchComboBox : ToolStripComboBox
	{
		[UsedImplicitly]
		public int MaximumWidth { get; set; }

		public override Size GetPreferredSize(Size constrainingSize)
		{
			Assert.NotNull(Owner, "ToolStripItem.Owner");

			// Use the default size if the combo box is on the overflow menu
			// or is on a vertical ToolStrip.
			if (IsOnOverflow || Owner.Orientation == Orientation.Vertical)
			{
				return DefaultSize;
			}

			// Maintain the total available width as it is calculated,
			// starting with the display width of the owning ToolStrip.
			int availableWidth = Owner.DisplayRectangle.Width;

			if (Owner.OverflowButton.Visible)
			{
				availableWidth -= Owner.OverflowButton.Width;
				availableWidth -= Owner.OverflowButton.Margin.Horizontal;
			}

			// Count the stretching combo boxes on this tool strip.
			var stretchBoxCount = 0;

			foreach (ToolStripItem item in Owner.Items)
			{
				if (item.IsOnOverflow)
				{
					continue;
				}

				if (item is ToolStripStretchComboBox)
				{
					// For ToolStripStretchComboBox items, increment the count and 
					// subtract the margin width from the total available width.
					stretchBoxCount++;
					availableWidth -= item.Margin.Horizontal;
				}
				else
				{
					// For all other items, subtract the full width from the total
					// available width.
					availableWidth = availableWidth - item.Width - item.Margin.Horizontal;
				}
			}

			// If there are multiple ToolStripStretchComboBox items in the owning
			// ToolStrip, divide the total available width between them. 
			if (stretchBoxCount > 1)
			{
				availableWidth /= stretchBoxCount;
			}

			// If the available width is less than the default width, use the
			// default width, forcing one or more items onto the overflow menu.
			if (availableWidth < DefaultSize.Width)
			{
				availableWidth = DefaultSize.Width;
			}

			if (MaximumWidth > 0 && availableWidth > MaximumWidth)
			{
				availableWidth = MaximumWidth;
			}

			// Retrieve the preferred size from the base class, but change the
			// width to the calculated width. 
			Size size = base.GetPreferredSize(constrainingSize);
			size.Width = availableWidth;

			return size;
		}
	}
}

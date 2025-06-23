using System;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	/// <summary>
	/// Helper methods for winforms comboboxes.
	/// </summary>
	public static class ComboBoxUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Gets the width of the drop down list that is sufficient to 
		/// display all items without cutting off part of the item text.
		/// </summary>
		/// <param name="comboBox">The combo box.</param>
		/// <param name="comboBoxDisplayName">Display name of the combobox (this can be 
		/// an arbitrary string to identify the combobox).</param>
		/// <returns>The width required so that all items fit in the drop down width. 
		/// Minimum returned width is the width of the combobox.</returns>
		public static int GetAutoFitDropDownWidth(
			[NotNull] ComboBox comboBox,
			[CanBeNull] string comboBoxDisplayName = null)
		{
			Assert.ArgumentNotNull(comboBox, nameof(comboBox));

			string displayName = string.IsNullOrEmpty(comboBoxDisplayName)
				                     ? comboBox.Name
				                     : comboBoxDisplayName;

			// Return current width if handle not created yet
			if (!comboBox.IsHandleCreated)
			{
				_msg.DebugFormat("Handle not yet created for combobox [{0}], returning current width", displayName);
				return comboBox.Width;
			}

			if (comboBox.Items.Count == 0)
			{
				return comboBox.Width;
			}

			Font font = comboBox.Font;

			using (Graphics g = comboBox.CreateGraphics())
			{
				var maxLength = 0;

				foreach (object item in comboBox.Items)
				{
					string text = comboBox.GetItemText(item);
					var length = (int) g.MeasureString(text, font).Width;

					if (length > maxLength)
					{
						maxLength = length;
					}
				}

				const int scrollbarOffset = 20;
				maxLength += scrollbarOffset;

				// make sure the width is not wider than the current working area
				Rectangle workingArea = Screen.GetWorkingArea(comboBox);
				if (workingArea.IsEmpty)
				{
					_msg.DebugFormat("Working area not found for combobox [{0}]", displayName);
					maxLength = comboBox.Width;
				}
				else
				{
					int workingAreaWidth = workingArea.Width;
					if (maxLength > workingAreaWidth)
					{
						maxLength = workingAreaWidth;
					}
				}

				return Math.Max(maxLength, comboBox.Width);

				//// Make sure we are inbounds of the screen 
				//int left = this.PointToScreen(new Point(0, this.Left)).X;
				//if (maxLength > Screen.PrimaryScreen.WorkingArea.Width - left)
				//    maxLength = Screen.PrimaryScreen.WorkingArea.Width - left;

				//comboBox.DropDownWidth = Math.Max(maxLength, comboBox.Width);
			}
		}
	}
}

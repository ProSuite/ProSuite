using System;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	/// <summary>
	/// Defines the editing control for the DataGridViewNumericUpDownCell custom cell type.
	/// </summary>
	internal class DataGridViewNumericUpDownEditingControl : NumericUpDown,
	                                                         IDataGridViewEditingControl
	{
		// Needed to forward keyboard messages to the child TextBox control.
		[DllImport("USER32.DLL", CharSet = CharSet.Auto)]
		private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam,
		                                         IntPtr lParam);

		// The grid that owns this editing control
		private DataGridView dataGridView;

		// Stores whether the editing control's value has changed or not
		private bool valueChanged;

		// Stores the row index in which the editing control resides
		private int rowIndex;

		/// <summary>
		/// Constructor of the editing control class
		/// </summary>
		public DataGridViewNumericUpDownEditingControl()
		{
			// The editing control must not be part of the tabbing loop
			TabStop = false;
		}

		// Beginning of the IDataGridViewEditingControl interface implementation

		/// <summary>
		/// Property which caches the grid that uses this editing control
		/// </summary>
		public virtual DataGridView EditingControlDataGridView
		{
			get { return dataGridView; }
			set { dataGridView = value; }
		}

		/// <summary>
		/// Property which represents the current formatted value of the editing control
		/// </summary>
		public virtual object EditingControlFormattedValue
		{
			get
			{
				return
					GetEditingControlFormattedValue(
						DataGridViewDataErrorContexts.Formatting);
			}
			set { Text = (string) value; }
		}

		/// <summary>
		/// Property which represents the row in which the editing control resides
		/// </summary>
		public virtual int EditingControlRowIndex
		{
			get { return rowIndex; }
			set { rowIndex = value; }
		}

		/// <summary>
		/// Property which indicates whether the value of the editing control has changed or not
		/// </summary>
		public virtual bool EditingControlValueChanged
		{
			get { return valueChanged; }
			set { valueChanged = value; }
		}

		/// <summary>
		/// Property which determines which cursor must be used for the editing panel,
		/// i.e. the parent of the editing control.
		/// </summary>
		public virtual Cursor EditingPanelCursor => Cursors.Default;

		/// <summary>
		/// Property which indicates whether the editing control needs to be repositioned 
		/// when its value changes.
		/// </summary>
		public virtual bool RepositionEditingControlOnValueChange => false;

		/// <summary>
		/// Method called by the grid before the editing control is shown so it can adapt to the 
		/// provided cell style.
		/// </summary>
		public virtual void ApplyCellStyleToEditingControl(
			DataGridViewCellStyle dataGridViewCellStyle)
		{
			Font = dataGridViewCellStyle.Font;
			if (dataGridViewCellStyle.BackColor.A < 255)
			{
				// The NumericUpDown control does not support transparent back colors
				Color opaqueBackColor =
					Color.FromArgb(255, dataGridViewCellStyle.BackColor);
				BackColor = opaqueBackColor;
				dataGridView.EditingPanel.BackColor = opaqueBackColor;
			}
			else
			{
				BackColor = dataGridViewCellStyle.BackColor;
			}

			ForeColor = dataGridViewCellStyle.ForeColor;
			TextAlign =
				DataGridViewNumericUpDownCell.TranslateAlignment(
					dataGridViewCellStyle.Alignment);
		}

		/// <summary>
		/// Method called by the grid on keystrokes to determine if the editing control is
		/// interested in the key or not.
		/// </summary>
		public virtual bool EditingControlWantsInputKey(Keys keyData,
		                                                bool dataGridViewWantsInputKey)
		{
			switch (keyData & Keys.KeyCode)
			{
				case Keys.Right:
				{
					var textBox = Controls[1] as TextBox;
					if (textBox != null)
					{
						// If the end of the selection is at the end of the string,
						// let the DataGridView treat the key message
						if ((RightToLeft == RightToLeft.No &&
						     ! (textBox.SelectionLength == 0 &&
						        textBox.SelectionStart == textBox.Text.Length)) ||
						    (RightToLeft == RightToLeft.Yes &&
						     ! (textBox.SelectionLength == 0 &&
						        textBox.SelectionStart == 0)))
						{
							return true;
						}
					}

					break;
				}

				case Keys.Left:
				{
					var textBox = Controls[1] as TextBox;
					if (textBox != null)
					{
						// If the end of the selection is at the begining of the string
						// or if the entire text is selected and we did not start editing,
						// send this character to the dataGridView, else process the key message
						if ((RightToLeft == RightToLeft.No &&
						     ! (textBox.SelectionLength == 0 &&
						        textBox.SelectionStart == 0)) ||
						    (RightToLeft == RightToLeft.Yes &&
						     ! (textBox.SelectionLength == 0 &&
						        textBox.SelectionStart == textBox.Text.Length)))
						{
							return true;
						}
					}

					break;
				}

				case Keys.Down:
					// If the current value hasn't reached its minimum yet, handle the key. Otherwise let
					// the grid handle it.
					if (Value > Minimum)
					{
						return true;
					}

					break;

				case Keys.Up:
					// If the current value hasn't reached its maximum yet, handle the key. Otherwise let
					// the grid handle it.
					if (Value < Maximum)
					{
						return true;
					}

					break;

				case Keys.Home:
				case Keys.End:
				{
					// Let the grid handle the key if the entire text is selected.
					var textBox = Controls[1] as TextBox;
					if (textBox != null)
					{
						if (textBox.SelectionLength != textBox.Text.Length)
						{
							return true;
						}
					}

					break;
				}

				case Keys.Delete:
				{
					// Let the grid handle the key if the carret is at the end of the text.
					var textBox = Controls[1] as TextBox;
					if (textBox != null)
					{
						if (textBox.SelectionLength > 0 ||
						    textBox.SelectionStart < textBox.Text.Length)
						{
							return true;
						}
					}

					break;
				}
			}

			return ! dataGridViewWantsInputKey;
		}

		/// <summary>
		/// Returns the current value of the editing control.
		/// </summary>
		public virtual object GetEditingControlFormattedValue(
			DataGridViewDataErrorContexts context)
		{
			bool userEdit = UserEdit;
			try
			{
				// Prevent the Value from being set to Maximum or Minimum when the cell is being painted.
				UserEdit = (context & DataGridViewDataErrorContexts.Display) == 0;
				return
					Value.ToString((ThousandsSeparator
						                ? "N"
						                : "F") +
					               DecimalPlaces);
			}
			finally
			{
				UserEdit = userEdit;
			}
		}

		/// <summary>
		/// Called by the grid to give the editing control a chance to prepare itself for
		/// the editing session.
		/// </summary>
		public virtual void PrepareEditingControlForEdit(bool selectAll)
		{
			var textBox = Controls[1] as TextBox;
			if (textBox != null)
			{
				if (selectAll)
				{
					textBox.SelectAll();
				}
				else
				{
					// Do not select all the text, but
					// position the caret at the end of the text
					textBox.SelectionStart = textBox.Text.Length;
				}
			}
		}

		// End of the IDataGridViewEditingControl interface implementation

		/// <summary>
		/// Small utility function that updates the local dirty state and 
		/// notifies the grid of the value change.
		/// </summary>
		private void NotifyDataGridViewOfValueChange()
		{
			if (! valueChanged)
			{
				valueChanged = true;
				dataGridView.NotifyCurrentCellDirty(true);
			}
		}

		/// <summary>
		/// Listen to the KeyPress notification to know when the value changed, and 
		/// notify the grid of the change.
		/// </summary>
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			base.OnKeyPress(e);

			// The value changes when a digit, the decimal separator, the group separator or
			// the negative sign is pressed.
			var notifyValueChange = false;
			if (char.IsDigit(e.KeyChar))
			{
				notifyValueChange = true;
			}
			else
			{
				NumberFormatInfo numberFormatInfo =
					CultureInfo.CurrentCulture.NumberFormat;
				string decimalSeparatorStr = numberFormatInfo.NumberDecimalSeparator;
				string groupSeparatorStr = numberFormatInfo.NumberGroupSeparator;
				string negativeSignStr = numberFormatInfo.NegativeSign;
				if (! string.IsNullOrEmpty(decimalSeparatorStr) &&
				    decimalSeparatorStr.Length == 1)
				{
					notifyValueChange = decimalSeparatorStr[0] == e.KeyChar;
				}

				if (! notifyValueChange && ! string.IsNullOrEmpty(groupSeparatorStr) &&
				    groupSeparatorStr.Length == 1)
				{
					notifyValueChange = groupSeparatorStr[0] == e.KeyChar;
				}

				if (! notifyValueChange && ! string.IsNullOrEmpty(negativeSignStr) &&
				    negativeSignStr.Length == 1)
				{
					notifyValueChange = negativeSignStr[0] == e.KeyChar;
				}
			}

			if (notifyValueChange)
			{
				// Let the DataGridView know about the value change
				NotifyDataGridViewOfValueChange();
			}
		}

		/// <summary>
		/// Listen to the ValueChanged notification to forward the change to the grid.
		/// </summary>
		protected override void OnValueChanged(EventArgs e)
		{
			base.OnValueChanged(e);
			if (Focused)
			{
				// Let the DataGridView know about the value change
				NotifyDataGridViewOfValueChange();
			}
		}

		/// <summary>
		/// A few keyboard messages need to be forwarded to the inner textbox of the
		/// NumericUpDown control so that the first character pressed appears in it.
		/// </summary>
		protected override bool ProcessKeyEventArgs(ref Message m)
		{
			var textBox = Controls[1] as TextBox;
			if (textBox != null)
			{
				SendMessage(textBox.Handle, m.Msg, m.WParam, m.LParam);
				return true;
			}

			return base.ProcessKeyEventArgs(ref m);
		}
	}
}

using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public partial class NumericUpDownNullable : UserControl
	{
		public event EventHandler ValueChanged;

		#region Constructors

		public NumericUpDownNullable()
		{
			InitializeComponent();
		}

		#endregion

		[Category("Appearance")]
		public decimal? Value
		{
			get
			{
				return _checkBoxNull.Checked
					       ? (decimal?) null
					       : _numericUpDown.Value;
			}
			set
			{
				if (value == null)
				{
					if (! _checkBoxNull.Checked)
					{
						_checkBoxNull.Checked = true;
					}
				}
				else
				{
					_numericUpDown.Value = value.Value;

					if (_checkBoxNull.Checked)
					{
						_checkBoxNull.Checked = false;
					}
				}
			}
		}

		[Category("Data")]
		public decimal Minimum
		{
			get { return _numericUpDown.Minimum; }
			set { _numericUpDown.Minimum = value; }
		}

		[Category("Data")]
		public decimal Maximum
		{
			get { return _numericUpDown.Maximum; }
			set { _numericUpDown.Maximum = value; }
		}

		[Category("Data")]
		public decimal Increment
		{
			get { return _numericUpDown.Increment; }
			set { _numericUpDown.Increment = value; }
		}

		[Category("Data")]
		public int DecimalPlaces
		{
			get { return _numericUpDown.DecimalPlaces; }
			set { _numericUpDown.DecimalPlaces = value; }
		}

		[Category("Data")]
		public bool ThousandsSeparator
		{
			get { return _numericUpDown.ThousandsSeparator; }
			set { _numericUpDown.ThousandsSeparator = value; }
		}

		public void Select(int start, int length)
		{
			_numericUpDown.Select(start, length);
		}

		#region Non-public members

		protected virtual void OnValueChanged(EventArgs e)
		{
			if (ValueChanged != null)
			{
				ValueChanged(this, e);
			}
		}

		#region Event handlers

		private void _numericUpDown_ValueChanged(object sender, EventArgs e)
		{
			OnValueChanged(EventArgs.Empty);
		}

		private void _checkBoxNull_CheckedChanged(object sender, EventArgs e)
		{
			bool isNull = _checkBoxNull.Checked;

			_numericUpDown.Enabled = ! isNull;
			_numericUpDown.Visible = ! isNull;

			_textBoxNull.Enabled = isNull;
			_textBoxNull.Visible = isNull;

			OnValueChanged(EventArgs.Empty);
		}

		private void _numericUpDown_KeyDown(object sender, KeyEventArgs e)
		{
			// propagate keyDown event from numericUpDown
			OnKeyDown(e);
		}

		#endregion

		#endregion
	}
}

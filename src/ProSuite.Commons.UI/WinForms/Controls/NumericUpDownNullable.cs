using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public partial class NumericUpDownNullable : UserControl
	{
		// Horizontal distance between the number control and the check box, in logical units.
		private const int _gapLogical = 6;

		private int _numberWidth;
		private bool _inLayoutChildren;

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

		/// <summary>
		/// The width of the number control, in pixels. 0 (the default) lets it fill the width
		/// remaining to the left of the "not set" check box, i.e. the entire control width minus
		/// what the check box needs at the current font.
		/// </summary>
		/// <remarks>
		/// Use this when the control sits in an auto-sizing layout (e.g. a TableLayoutPanel
		/// column) and should be no wider than the number needs: the preferred size then is this
		/// width plus the check box, instead of the design-time width.
		/// </remarks>
		[Category("Layout")]
		[DefaultValue(0)]
		public int NumberWidth
		{
			get { return _numberWidth; }
			set
			{
				if (_numberWidth == value)
				{
					return;
				}

				_numberWidth = value;

				PerformLayout();
			}
		}

		public void Select(int start, int length)
		{
			_numericUpDown.Select(start, length);
		}

		public override Size GetPreferredSize(Size proposedSize)
		{
			Size checkBoxSize = _checkBoxNull.PreferredSize;

			int width = _numberWidth > 0
				            ? _numberWidth + Gap + checkBoxSize.Width
				            : base.GetPreferredSize(proposedSize).Width;

			int height = Math.Max(_numericUpDown.PreferredHeight, checkBoxSize.Height);

			return new Size(width + Padding.Horizontal, height + Padding.Vertical);
		}

		#region Non-public members

		private int Gap => LogicalToDeviceUnits(_gapLogical);

		protected override void OnLayout(LayoutEventArgs e)
		{
			base.OnLayout(e);

			LayoutChildren();
		}

		/// <summary>
		/// Positions the child controls: the number control on the left, the "not set" check box
		/// right of it, both vertically centered.
		/// </summary>
		/// <remarks>
		/// Done here rather than by anchoring because the check box is auto-sized: its width
		/// depends on the font, and any fixed distance assumed for it (as the designer-generated
		/// bounds do) makes it overlap the number control's spin buttons at larger fonts or
		/// higher DPI scaling.
		/// </remarks>
		private void LayoutChildren()
		{
			if (_inLayoutChildren || _checkBoxNull == null || _numericUpDown == null ||
			    _textBoxNull == null)
			{
				return;
			}

			_inLayoutChildren = true;
			try
			{
				Rectangle area = DisplayRectangle;

				Size checkBoxSize = _checkBoxNull.PreferredSize;

				int available = Math.Max(0, area.Width - checkBoxSize.Width - Gap);

				int numberWidth = _numberWidth > 0
					                  ? Math.Min(_numberWidth, available)
					                  : available;

				int numberHeight = _numericUpDown.PreferredHeight;

				_numericUpDown.SetBounds(area.Left, VerticalCenter(area, numberHeight),
				                         numberWidth, numberHeight);
				_textBoxNull.SetBounds(area.Left, VerticalCenter(area, numberHeight),
				                       numberWidth, numberHeight);

				_checkBoxNull.SetBounds(area.Left + numberWidth + Gap,
				                        VerticalCenter(area, checkBoxSize.Height),
				                        checkBoxSize.Width, checkBoxSize.Height);
			}
			finally
			{
				_inLayoutChildren = false;
			}
		}

		private static int VerticalCenter(Rectangle area, int height)
		{
			return area.Top + Math.Max(0, (area.Height - height) / 2);
		}

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
			// Important when initializing the control: Update number displays even if check box is
			// not changed. Otherwise on .net 6 the check box will be displayed in the wrong location.
			UpdateNumberControlsVisibility(_checkBoxNull.Checked);

			OnValueChanged(EventArgs.Empty);
		}

		private void _checkBoxNull_CheckedChanged(object sender, EventArgs e)
		{
			UpdateNumberControlsVisibility(_checkBoxNull.Checked);

			OnValueChanged(EventArgs.Empty);
		}

		private void UpdateNumberControlsVisibility(bool isNull)
		{
			_numericUpDown.Enabled = ! isNull;
			_numericUpDown.Visible = ! isNull;

			_textBoxNull.Enabled = isNull;
			_textBoxNull.Visible = isNull;
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

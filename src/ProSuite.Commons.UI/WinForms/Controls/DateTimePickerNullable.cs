using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	/// <summary>
	/// This class extends the DateTimePicker class to allow null values
	/// for the date. Most of the code is from
	/// http://www.codeproject.com/cs/miscctrl/NullableDateTimePicker.asp
	/// 
	/// It displays a null date according the fiels _nullValue.
	/// </summary>
	public class DateTimePickerNullable : DateTimePicker, INotifyPropertyChanged
	{
		// The custom format of the DateTimePicker control
		private SolidBrush _backColorBrush = new SolidBrush(SystemColors.Window);
		private DateTimePickerFormat _format = DateTimePickerFormat.Long;

		// The format of the DateTimePicker control as string
		private string _formatAsString;

		// true, when no date shall be displayed (empty DateTimePicker)
		private bool _isNull;

		private object _originalValue;

		/// <summary>
		/// Indicates that the calendar is currently dropped down
		/// </summary>
		private bool _calendarIsDroppedDown;

		/// <summary>
		/// Indicates that the time part of the date is currently being dropped (latch flag).
		/// </summary>
		private bool _forcingValueChange;

		// If _isNull = true, this value is shown in the DTP

		#region Constructors

		public DateTimePickerNullable()
		{
			base.Format = DateTimePickerFormat.Custom;
			NullValue = " ";
			Format = DateTimePickerFormat.Short;
		}

		#endregion

		public bool ReadOnly { get; set; }

		public void Render([NotNull] Action<DateTimePickerNullable> procedure)
		{
			Assert.ArgumentNotNull(procedure, nameof(procedure));

			bool origReadOnly = ReadOnly;

			try
			{
				ReadOnly = false;

				procedure(this);
			}
			finally
			{
				ReadOnly = origReadOnly;
				_originalValue = Value;
			}
		}

		public new bool Checked
		{
			get { return base.Checked; }
			set
			{
				if (value == Checked)
				{
					return;
				}

				OnPropertyChanged("BindableValue");
			}
		}

		public new object Value
		{
			get
			{
				return _isNull
					       ? (object) null
					       : base.Value;
			}
			set
			{
				if (value == null || value == DBNull.Value)
				{
					SetToNullValue();
				}
				else
				{
					SetToDateTimeValue();
					base.Value = (DateTime) value;
				}
			}
		}

		public new string CustomFormat { get; set; }

		public new DateTimePickerFormat Format
		{
			get { return _format; }
			set
			{
				_format = value;

				if (! _isNull)
				{
					SetFormat();
				}

				OnFormatChanged(EventArgs.Empty);
			}
		}

		public override Color BackColor
		{
			get { return base.BackColor; }
			set
			{
				_backColorBrush?.Dispose();

				base.BackColor = value;
				_backColorBrush = new SolidBrush(value);

				Invalidate();
			}
		}

		protected string FormatAsString
		{
			get { return _formatAsString; }
			set
			{
				_formatAsString = value;
				base.CustomFormat = value;
			}
		}

		public string NullValue { get; set; }

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		private void SetToDateTimeValue()
		{
			if (! _isNull)
			{
				return;
			}

			SetFormat();

			_isNull = false;

			base.OnValueChanged(new EventArgs());
		}

		private void SetFormat()
		{
			CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
			DateTimeFormatInfo dateTimeFormat = currentCulture.DateTimeFormat;

			switch (_format)
			{
				case DateTimePickerFormat.Long:
					FormatAsString = dateTimeFormat.LongDatePattern;
					break;

				case DateTimePickerFormat.Short:
					FormatAsString = dateTimeFormat.ShortDatePattern;
					break;

				case DateTimePickerFormat.Time:
					FormatAsString = dateTimeFormat.ShortTimePattern;
					break;

				case DateTimePickerFormat.Custom:
					FormatAsString = CustomFormat;
					break;
			}
		}

		private void SetToNullValue()
		{
			_isNull = true;
			base.CustomFormat = string.IsNullOrEmpty(NullValue)
				                    ? " "
				                    : "'" + NullValue + "'";
		}

		protected override void WndProc(ref Message m)
		{
			const int WM_NOTIFY = 0x4e;
			const int WM_ERASEBKGND = 20;

			if (m.Msg == WM_ERASEBKGND)
			{
				// NOTE: this no longer works under windows 7 with visual styles (-> arcgis 10)
				// There are partial solutions to this (e.g. http://www.codeproject.com/Articles/30660/A-DateTimePicker-with-working-BackColor)
				// but so far none was found that fulfills all requirements (the example does not allow manual input of date/time)
				using (Graphics g = Graphics.FromHdc(m.WParam))
				{
					g.FillRectangle(_backColorBrush, ClientRectangle);
				}

				return;
			}

			if (m.Msg == WM_NOTIFY)
			{
				var nm = (NMHDR) m.GetLParam(typeof(NMHDR));

				if (nm.Code == -746 || nm.Code == -722) // DTN_CLOSEUP || DTN_?
				{
					if (! ReadOnly)
					{
						SetToDateTimeValue();
					}
				}
			}

			base.WndProc(ref m);
		}

		protected override void OnDropDown(EventArgs e)
		{
			base.OnDropDown(e);

			_calendarIsDroppedDown = true;
		}

		protected override void OnValueChanged(EventArgs e)
		{
			if (_forcingValueChange)
			{
				return;
			}

			if (ReadOnly)
			{
				_forcingValueChange = true;

				try
				{
					Value = _originalValue;
				}
				finally
				{
					_forcingValueChange = false;
				}
			}
			else
			{
				if (_calendarIsDroppedDown)
				{
					_forcingValueChange = true;

					try
					{
						base.Value = base.Value.Date;
					}
					finally
					{
						_forcingValueChange = false;
					}
				}
			}

			if (! Equals(Value, _originalValue))
			{
				base.OnValueChanged(e);

				_originalValue = Value;
			}
		}

		protected override void OnCloseUp(EventArgs e)
		{
			base.OnCloseUp(e);

			_calendarIsDroppedDown = false;

			OnPropertyChanged("BindableValue");
		}

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#region Nested type: NMHDR

		[StructLayout(LayoutKind.Sequential)]
		private struct NMHDR
		{
			[UsedImplicitly] public IntPtr HwndFrom;

			[UsedImplicitly] public int IdFrom;

			[UsedImplicitly] public int Code;
		}

		#endregion
	}
}

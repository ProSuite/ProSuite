using System;
using System.Windows.Forms;
using ProSuite.Commons.Misc;

namespace ProSuite.DdxEditor.Content.Projects
{
	public partial class DeleteVerificationsBeforeDateForm : Form
	{
		private readonly Latch _latch = new Latch();

		public DeleteVerificationsBeforeDateForm()
		{
			InitializeComponent();

			_latch.RunInsideLatch(
				() =>
				{
					_radioButtonOlderThanMonths.Checked = true;
					_numericUpDownMonths.Value = 12;
					UpdateRelativeDate();
				});
		}

		public DateTime SelectedDate => _monthCalendar.SelectionStart;

		private int MonthsBeforeNow => Convert.ToInt32(_numericUpDownMonths.Value);

		private void DateSelected()
		{
			_radioButtonOlderThanSpecificDate.Checked = true;
			_numericUpDownMonths.Enabled = false;
		}

		private void UpdateRelativeDate()
		{
			DateTime now = DateTime.Now;
			DateTime relativeDate = now.AddMonths(-1 * MonthsBeforeNow);

			_monthCalendar.SetDate(relativeDate);
		}

		private void _monthCalendar_DateChanged(object sender, DateRangeEventArgs e)
		{
			if (_latch.IsLatched)
			{
				return;
			}

			_latch.RunInsideLatch(DateSelected);
		}

		private void _radioButtonOlderThanMonths_CheckedChanged(object sender, EventArgs e)
		{
			if (_latch.IsLatched)
			{
				return;
			}

			if (_radioButtonOlderThanMonths.Checked)
			{
				_numericUpDownMonths.Enabled = true;
				_latch.RunInsideLatch(UpdateRelativeDate);
			}
		}

		private void _numericUpDownMonths_ValueChanged(object sender, EventArgs e)
		{
			if (_latch.IsLatched)
			{
				return;
			}

			_latch.RunInsideLatch(UpdateRelativeDate);
		}

		private void _radioButtonOlderThanSpecificDate_CheckedChanged(object sender,
			EventArgs e)
		{
			if (_latch.IsLatched)
			{
				return;
			}

			if (_radioButtonOlderThanSpecificDate.Checked)
			{
				_numericUpDownMonths.Enabled = false;
			}
		}

		private void _buttonOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}
	}
}

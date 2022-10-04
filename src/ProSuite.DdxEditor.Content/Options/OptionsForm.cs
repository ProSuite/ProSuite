using System;
using System.Windows.Forms;

namespace ProSuite.DdxEditor.Content.Options
{
	public partial class OptionsForm : Form
	{
		public OptionsForm()
		{
			InitializeComponent();
		}

		public bool ShowDeletedModelElements
		{
			get { return _checkBoxIncludeDeletedModelElements.Checked; }
			set { _checkBoxIncludeDeletedModelElements.Checked = value; }
		}

		public bool ShowQualityConditionsBasedOnDeletedDatasets
		{
			get { return _checkBoxIncludeQualityConditionsBasedOnDeletedDatasets.Checked; }
			set { _checkBoxIncludeQualityConditionsBasedOnDeletedDatasets.Checked = value; }
		}

		public bool ListQualityConditionsWithDataset
		{
			get { return _checkBoxListQualityConditionsWithDataset.Checked; }
			set { _checkBoxListQualityConditionsWithDataset.Checked = value; }
		}

		private void _buttonCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void _buttonOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}
	}
}

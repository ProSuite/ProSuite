using System;
using System.Windows.Forms;

namespace ProSuite.Commons.UI.PropertyEditors
{
	internal partial class EditorForm : Form
	{
		private Type _type;
		private object _value;

		public EditorForm(object initValue)
		{
			InitializeComponent();

			Value = initValue;

			propertyGrid.ToolbarVisible = false;
		}

		public object Value
		{
			get { return _value; }
			set
			{
				_value = value;
				propertyGrid.SelectedObject = _value;
				InitType();
				propertyGrid.ExpandAllGridItems();
			}
		}

		public Type PropertyType => _type;

		private void InitType()
		{
			_type = _value.GetType();
			textBoxObject.Text = _value.ToString();
			// TODO: get Description
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Hide();
		}

		private void buttonOk_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Hide();
		}
	}
}

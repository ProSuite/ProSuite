using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.UI.QA.PropertyEditors
{
	public class DatasetClearEditor : UITypeEditor
	{
		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
		{
			return UITypeEditorEditStyle.Modal;
		}

		public override object EditValue(ITypeDescriptorContext context,
		                                 IServiceProvider provider,
		                                 object value)
		{
			if (value == null)
			{
				if (context == null)
				{
					return null;
				}

				Type t = context.PropertyDescriptor?.PropertyType;
				if (t == null)
				{
					return null;
				}

				var c = Activator.CreateInstance(t) as DatasetConfig;
				if (c == null)
				{
					return null;
				}

				c.SetTestParameterValue(
					new DatasetTestParameterValue(context.PropertyDescriptor.Name, null));
				return c;
			}

			if (! (value is DatasetConfig))
			{
				return value;
			}

			DialogResult result = MessageBox.Show(@"Do you want to clear the dataset?",
			                                      @"Clear Dataset",
			                                      MessageBoxButtons.YesNoCancel,
			                                      MessageBoxIcon.Question,
			                                      MessageBoxDefaultButton.Button3);

			return result != DialogResult.Yes ? value : null;
		}
	}
}

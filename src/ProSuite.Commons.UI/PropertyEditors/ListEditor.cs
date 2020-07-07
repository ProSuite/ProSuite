using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.PropertyEditors
{
	public class ListEditor : UITypeEditor
	{
		private readonly bool _readOnly;

		[UsedImplicitly]
		public ListEditor() : this(readOnly : false) { }

		protected ListEditor(bool readOnly)
		{
			_readOnly = readOnly;
		}

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
		{
			return UITypeEditorEditStyle.DropDown;
		}

		public override object EditValue(
			ITypeDescriptorContext context,
			IServiceProvider provider,
			object value)
		{
			Assert.ArgumentNotNull(provider, nameof(provider));
			Assert.ArgumentNotNull(context, nameof(context));

			var editorService = provider.GetService(typeof(IWindowsFormsEditorService))
				                    as IWindowsFormsEditorService;

			if (editorService == null)
			{
				return value;
			}

			PropertyDescriptor descriptor = Assert.NotNull(context.PropertyDescriptor,
			                                               "property descriptor is null");

			Type propertyType = descriptor.PropertyType.GetGenericArguments()[0];

			using (var form = new ListEditorForm((IList) value,
			                                     propertyType,
			                                     context,
			                                     _readOnly))
			{
				// try to set the owner, to prevent the owner form to move 
				// to the background when the dialog is closed. 
				// Since the form.Owner property is of type Form, this 
				// is only possible if the parent is a form.
				// TODO this leads to message "a form cannot be owned or parented by itself" from property grid
				//if (form.Owner == null)
				//{
				//    var defaultOwner = UIEnvironment.MainWindow as Form;
				//    if (defaultOwner != null)
				//    {
				//        form.Owner = defaultOwner;
				//    }
				//}

				DialogResult result;
				try
				{
					form.DataChanged += form_DataChanged;

					result = editorService.ShowDialog(form);
				}
				finally
				{
					form.DataChanged -= form_DataChanged;
				}

				if (result == DialogResult.OK)
				{
					value = form.GetNewValue();
				}
				else
				{
					if (value != null)
					{
						// undo any changes made

						foreach (object item in (IList) value)
						{
							var revert = item as IRevertibleChangeTracking;
							if (revert != null && revert.IsChanged)
							{
								revert.RejectChanges();
							}
						}
					}
				}
			}

			return value;
		}

		private static void form_DataChanged(object sender, EventArgs e)
		{
			var editor = (ListEditorForm) sender;

			var changed = editor.Context.Instance as IDataChanged;

			changed?.OnDataChanged(e);
		}
	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.UI.Finder;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.UI.Core.QA.BoundTableRows;

namespace ProSuite.UI.QA.PropertyEditors
{
	public class DatasetEditor : UITypeEditor
	{
		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
		{
			return UITypeEditorEditStyle.Modal | UITypeEditorEditStyle.DropDown;
		}

		public override object EditValue(ITypeDescriptorContext context,
		                                 IServiceProvider provider,
		                                 object value)
		{
			IWindowsFormsEditorService editorService =
				provider?.GetService(typeof(IWindowsFormsEditorService)) as
					IWindowsFormsEditorService;

			if (editorService == null)
			{
				return value;
			}

			Assert.NotNull(context, "context is null");

			var dsConfig = value as DatasetConfig;

			if (dsConfig == null)
			{
				dsConfig = (DatasetConfig) context.Instance;
			}
			else
			{
				if (context.Instance is TestConfigurator testConfigurator)
				{
					dsConfig.DatasetProvider = testConfigurator.DatasetProvider;
					dsConfig.QualityCondition = testConfigurator.QualityCondition;
				}
			}

			using (FinderForm<DatasetFinderItem> form = dsConfig.GetFinderForm())
			{
				DialogResult result = editorService.ShowDialog(form);

				if (result != DialogResult.OK)
				{
					return value;
				}

				IList<DatasetFinderItem> selection = form.Selection;

				if (selection != null && selection.Count == 1)
				{
					Dataset selectedDataset = selection[0].Dataset;

					if (value is DatasetConfig datasetConfig)
					{
						datasetConfig.Data = selectedDataset;
					}
					else
					{
						value = selectedDataset;
					}
				}

				return value;
			}
		}
	}
}

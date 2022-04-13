using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Env;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Content.QA.TestDescriptors;
using ProSuite.DdxEditor.Framework;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public class ImportQualitySpecificationCommand :
		ExchangeQualitySpecificationCommand<QualitySpecificationItem>
	{
		private static readonly Image _image;

		static ImportQualitySpecificationCommand()
		{
			_image = Resources.Import;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ImportQualitySpecificationCommand"/> class.
		/// </summary>
		/// <param name="qualitySpecificationItem">The quality specification item.</param>
		/// <param name="applicationController">The application controller.</param>
		public ImportQualitySpecificationCommand(
			[NotNull] QualitySpecificationItem qualitySpecificationItem,
			[NotNull] IApplicationController applicationController)
			: base(qualitySpecificationItem, applicationController) { }

		public override Image Image => _image;

		public override string Text => "Import...";

		protected override bool EnabledCore => base.EnabledCore && ! Item.IsDirty;

		protected override void ExecuteCore()
		{
			try
			{
				using (
					var form = new ImportQualitySpecificationsForm(FileFilter, DefaultExtension))
				{
					new ImportQualitySpecificationsController(form);

					DialogResult result = UIEnvironment.ShowDialog(form);

					if (result != DialogResult.OK)
					{
						return;
					}

					ApplicationController.GoToItem(Item);

					Item.ImportQualitySpecification(
						form.FilePath,
						form.IgnoreQualityConditionsForUnknownDatasets,
						form.UpdateTestDescriptorNames,
						form.UpdateTestDescriptorProperties);
				}

				ApplicationController.RefreshItem(Item);
				ApplicationController.RefreshFirstItem<QualityConditionsItem>();
				ApplicationController.RefreshFirstItem<TestDescriptorsItem>();
			}
			finally
			{
				ApplicationController.ReloadCurrentItem();
			}
		}
	}
}

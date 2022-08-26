using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Env;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public class ImportQualitySpecificationsCommand : ExchangeQualitySpecificationCommand<Item>
	{
		[NotNull] private static readonly Image _image;
		[NotNull] private readonly IQualitySpecificationContainer _container;

		static ImportQualitySpecificationsCommand()
		{
			_image = Resources.Import;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ImportQualitySpecificationsCommand"/> class.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="applicationController">The application controller.</param>
		/// <param name="container">The quality specifications container</param>
		public ImportQualitySpecificationsCommand(
			[NotNull] Item item,
			[NotNull] IApplicationController applicationController,
			[NotNull] IQualitySpecificationContainer container)
			: base(item, applicationController)
		{
			_container = container;
		}

		public override Image Image => _image;

		public override string Text => "Import Quality Specifications...";

		protected override void ExecuteCore()
		{
			try
			{
				using (var form = new ImportQualitySpecificationsForm(FileFilter,
					       DefaultExtension))
				{
					new ImportQualitySpecificationsController(form);

					DialogResult result = UIEnvironment.ShowDialog(form);

					if (result != DialogResult.OK)
					{
						return;
					}

					ApplicationController.GoToItem(Item);

					_container.ImportQualitySpecifications(
						form.FilePath,
						form.IgnoreQualityConditionsForUnknownDatasets,
						form.UpdateDescriptorNames,
						form.UpdateDescriptorProperties);

					ApplicationController.RefreshFirstItem<QAItem>();
				}
			}
			finally
			{
				ApplicationController.ReloadCurrentItem();
			}
		}
	}
}

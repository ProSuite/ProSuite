using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Content.QA.QSpec;
using ProSuite.DdxEditor.Framework;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	public class ImportTestDescriptorsCommand :
		ExchangeQualitySpecificationCommand<TestDescriptorsItem>
	{
		private static readonly Image _image;
		private readonly string _defaultTestDescriptorsXmlFile;

		/// <summary>
		/// Initializes the <see cref="ImportQualitySpecificationsCommand"/> class.
		/// </summary>
		static ImportTestDescriptorsCommand()
		{
			_image = Resources.Import;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ImportQualitySpecificationsCommand"/> class.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="applicationController">The application controller.</param>
		/// <param name="defaultTestDescriptorsXmlFile"></param>
		public ImportTestDescriptorsCommand([NotNull] TestDescriptorsItem item,
											[NotNull] IApplicationController
												applicationController,
											[CanBeNull] string defaultTestDescriptorsXmlFile)
			: base(item, applicationController)
		{
			_defaultTestDescriptorsXmlFile = defaultTestDescriptorsXmlFile;
		}

		public override Image Image => _image;

		public override string Text => string.Format("Import {0}...", Item.Text);

		protected override void ExecuteCore()
		{
			try
			{
				using (var dialog = new OpenFileDialog())
				{
					dialog.Multiselect = false;

					string initialFileName = _defaultTestDescriptorsXmlFile != null &&
											 File.Exists(_defaultTestDescriptorsXmlFile)
												 ? _defaultTestDescriptorsXmlFile
												 : null;

					string xmlFilePath = GetSelectedFileName(dialog, initialFileName);

					if (!string.IsNullOrEmpty(xmlFilePath))
					{
						Item.ImportTestDescriptors(xmlFilePath);
					}

					ApplicationController.RefreshFirstItem<TestDescriptorsItem>();
				}
			}
			finally
			{
				ApplicationController.ReloadCurrentItem();
			}
		}
	}
}

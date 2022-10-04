using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Content.QA.QSpec;
using ProSuite.DdxEditor.Framework;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
	public class ImportInstanceDescriptorsCommand :
		ExchangeQualitySpecificationCommand<AlgorithmDescriptorsItem>
	{
		private static readonly Image _image;
		private readonly string _defaultDescriptorsXmlFile;

		static ImportInstanceDescriptorsCommand()
		{
			_image = Resources.Import;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ImportInstanceDescriptorsCommand"/> class.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="applicationController">The application controller.</param>
		/// <param name="defaultDescriptorsXmlFile"></param>
		public ImportInstanceDescriptorsCommand(
			[NotNull] AlgorithmDescriptorsItem item,
			[NotNull] IApplicationController applicationController,
			[CanBeNull] string defaultDescriptorsXmlFile)
			: base(item, applicationController)
		{
			_defaultDescriptorsXmlFile = defaultDescriptorsXmlFile;
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

					string initialFileName = _defaultDescriptorsXmlFile != null &&
					                         File.Exists(_defaultDescriptorsXmlFile)
						                         ? _defaultDescriptorsXmlFile
						                         : null;

					string xmlFilePath = GetSelectedFileName(dialog, initialFileName);

					if (! string.IsNullOrEmpty(xmlFilePath))
					{
						Item.ImportInstanceDescriptors(xmlFilePath);
					}

					ApplicationController.RefreshFirstItem<AlgorithmDescriptorsItem>();
				}
			}
			finally
			{
				ApplicationController.ReloadCurrentItem();
			}
		}
	}
}

using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	internal class CreateQualitySpecificationReportCommand
		: ExchangeCommand<QualitySpecificationItem>
	{
		public const string DefaultExtension = "html";

		public const string FileFilter = "Html files (*.html)|*.html";

		[NotNull] private static readonly Image _image;
		[NotNull] private readonly string _reportTemplate;

		static CreateQualitySpecificationReportCommand()
		{
			_image = Resources.CreateReport;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CreateQualitySpecificationReportCommand"/> class.
		/// </summary>
		/// <param name="qualitySpecificationItem">The quality specification item.</param>
		/// <param name="applicationController">The application controller.</param>
		/// <param name="reportTemplate"></param>
		public CreateQualitySpecificationReportCommand(
			[NotNull] QualitySpecificationItem qualitySpecificationItem,
			[NotNull] IApplicationController applicationController,
			[NotNull] string reportTemplate)
			: base(
				qualitySpecificationItem, applicationController, DefaultExtension, FileFilter)
		{
			Assert.ArgumentNotNullOrEmpty(reportTemplate, nameof(reportTemplate));
			Assert.ArgumentCondition(File.Exists(reportTemplate),
			                         "report template does not exist: {0}",
			                         (object) reportTemplate);

			_reportTemplate = reportTemplate;
		}

		public override Image Image => _image;

		public override string Text => "Create Report...";

		public override string ToolTip => Text;

		protected override bool EnabledCore => ! Item.IsDirty;

		protected override void ExecuteCore()
		{
			// TODO revise initial directory
			// TODO open dialog with options
			// - non-default template to use
			// - include UUIDs
			// - options (file?) for categories
			// - language (german, english)

			using (FileDialog dialog = new SaveFileDialog())
			{
				string baseName = FileSystemUtils.ReplaceInvalidFileNameChars(Item.Text, '_');
				string initialFileName = string.Format("{0}.html", baseName);

				string htmlFilePath = GetSelectedFileName(dialog, initialFileName);

				if (! string.IsNullOrEmpty(htmlFilePath))
				{
					Item.CreateReport(htmlFilePath, _reportTemplate, overwrite: true);
				}
			}
		}
	}
}

using System.Drawing;
using System.Windows.Forms;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	internal class CreateReportForTestDescriptorsCommand
		: ExchangeCommand<TestDescriptorsItem>
	{
		public const string DefaultExtension = "html";

		public const string FileFilter = "Html files (*.html)|*.html";

		private static readonly Image _image;

		static CreateReportForTestDescriptorsCommand()
		{
			_image = Resources.CreateReport;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CreateReportForTestDescriptorsCommand"/> class.
		/// </summary>
		/// <param name="testDescriptorsItem">The test descriptors item.</param>
		/// <param name="applicationController">The application controller.</param>
		public CreateReportForTestDescriptorsCommand(
			TestDescriptorsItem testDescriptorsItem,
			IApplicationController applicationController)
			: base(testDescriptorsItem, applicationController, DefaultExtension, FileFilter) { }

		public override Image Image => _image;

		public override string Text => "Create Report for Registered Test Descriptors...";

		public override string ToolTip => Text;

		protected override bool EnabledCore => ! Item.IsDirty;

		protected override void ExecuteCore()
		{
			using (FileDialog dialog = new SaveFileDialog())
			{
				string htmlFilePath = GetSelectedFileName(dialog);

				if (! string.IsNullOrEmpty(htmlFilePath))
				{
					Item.CreateTestReport(htmlFilePath, true);
				}
			}
		}
	}
}

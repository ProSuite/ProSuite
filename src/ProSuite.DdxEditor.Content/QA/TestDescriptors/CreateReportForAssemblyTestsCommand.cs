using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	internal class CreateReportForAssemblyTestsCommand :
		ExchangeCommand<TestDescriptorsItem>
	{
		public const string DefaultExtension = "html";

		public const string FileFilter = "Html files (*.html)|*.html";

		private static readonly Image _image;

		static CreateReportForAssemblyTestsCommand()
		{
			_image = Resources.CreateReport;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CreateReportForAssemblyTestsCommand"/> class.
		/// </summary>
		/// <param name="testDescriptorsItem">The quality specification item.</param>
		/// <param name="applicationController">The application controller.</param>
		public CreateReportForAssemblyTestsCommand(
			TestDescriptorsItem testDescriptorsItem,
			IApplicationController applicationController)
			: base(testDescriptorsItem, applicationController, DefaultExtension, FileFilter) { }

		public override Image Image => _image;

		public override string Text => "Create Report For Tests in a .Net Assembly...";

		protected override bool EnabledCore => ! Item.IsDirty;

		protected override void ExecuteCore()
		{
			string dllFilePath = TestAssemblyUtils.ChooseAssemblyFileName();

			if (dllFilePath == null)
			{
				return;
			}

			Assembly assembly = Assembly.LoadFile(dllFilePath);

			string location = assembly.Location;
			Assert.NotNull(location, "assembly location is null");

			using (FileDialog dialog = new SaveFileDialog())
			{
				dialog.FileName = string.Format("{0}.html",
				                                Path.GetFileNameWithoutExtension(location));

				string htmlFilePath = GetSelectedFileName(dialog);

				if (! string.IsNullOrEmpty(htmlFilePath))
				{
					Item.CreateTestReport(assembly, htmlFilePath, true);
				}
			}
		}
	}
}

using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.DdxEditor.Framework.Help
{
	public class OpenDirectoryHelpProvider : HelpProviderBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly string _directoryPath;

		public OpenDirectoryHelpProvider([NotNull] string name, [CanBeNull] string directoryPath) :
			base(name)
		{
			_directoryPath = directoryPath;
		}

		protected override bool DetermineIfHelpIsAvailable()
		{
			if (_directoryPath == null)
			{
				_msg.DebugFormat("No directory specified");
				return false;
			}

			if (! Directory.Exists(_directoryPath))
			{
				_msg.DebugFormat("The directory does not exist: {0}", _directoryPath);
				return false;
			}

			return true;
		}

		protected override void ShowHelpCore(IWin32Window owner)
		{
			Assert.ArgumentNotNull(owner, nameof(owner));
			Assert.True(CanShowHelp, "Unable to show help directory");

			var startInfo = new ProcessStartInfo
			                {
				                FileName = "explorer.exe",
				                Arguments = _directoryPath,
				                ErrorDialogParentHandle = owner.Handle,
				                ErrorDialog = true
			                };

			Process.Start(startInfo);
		}
	}
}

using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.DdxEditor.Framework.Help
{
	public abstract class FilebasedHelpProviderBase : HelpProviderBase
	{
		[NotNull] private readonly string _expectedExtension;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected FilebasedHelpProviderBase([NotNull] string name,
		                                    [NotNull] string expectedExtension,
		                                    [CanBeNull] string filePath) : base(name)
		{
			Assert.ArgumentNotNullOrEmpty(expectedExtension, nameof(expectedExtension));

			FilePath = filePath;
			_expectedExtension = expectedExtension;
		}

		[CanBeNull]
		[PublicAPI]
		protected string FilePath { get; }

		protected override void ShowHelpCore(IWin32Window owner)
		{
			Assert.ArgumentNotNull(owner, nameof(owner));
			Assert.True(CanShowHelp, "Unable to show help file: {0}", FilePath);

			// NOTE: In .net 6 UseShellExecute defaults to false, which results in Win32Exception
			var startInfo = new ProcessStartInfo
			                {
				                FileName = Assert.NotNull(FilePath),
				                ErrorDialogParentHandle = owner.Handle,
				                ErrorDialog = true,
				                UseShellExecute = true
			                };

			Process.Start(startInfo);
		}

		protected override bool DetermineIfHelpIsAvailable()
		{
			if (FilePath == null)
			{
				_msg.DebugFormat("No file path specified");
				return false;
			}

			if (! File.Exists(FilePath))
			{
				_msg.DebugFormat("The file does not exist: {0}", FilePath);
				return false;
			}

			if (string.Equals(Path.GetExtension(FilePath), _expectedExtension,
			                  StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			_msg.DebugFormat("The file does not have the expected extension ({0}): {1}",
			                 _expectedExtension, FilePath);
			return false;
		}
	}
}

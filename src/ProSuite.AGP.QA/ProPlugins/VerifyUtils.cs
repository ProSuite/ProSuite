using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using ProSuite.AGP.WorkList;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;

namespace ProSuite.AGP.QA.ProPlugins
{
	public static class VerifyUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Name of the environment variable that provides the default base folder for verification
		/// results (reports and issue.gdb) when no explicit result path has been set.
		/// This is now obsolete, but still supported for backward compatibility. The new way to
		/// set the default result path is via the QualityVerificationOptions.ResultPathMode setting.
		/// </summary>
		public const string ResultPathEnvVariable = "PROSUITE_VERIFICATION_RESULT_DIR";

		/// <summary>
		/// Creates the shared command runner for the project-workspace based standard commands.
		/// </summary>
		[NotNull]
		public static VerificationCommandRunner CreateCommandRunner(
			[NotNull] IVerificationSessionContext sessionContext,
			[NotNull] IWorkListOpener workListOpener,
			[CanBeNull] Func<IQualityVerificationResult, ErrorDeletionInPerimeter, bool, Task<int>>
				saveAction)
		{
			return new VerificationCommandRunner(
				new ProjectWorkspaceVerificationCommandContext(
					sessionContext, workListOpener, saveAction));
		}

		public static void ShowProgressWindow(
			[NotNull] Window window,
			[NotNull] IQualitySpecificationReference qualitySpecification,
			[NotNull] string backendName,
			[NotNull] string actionTitle)
		{
			_msg.InfoFormat("{0}: {1}", qualitySpecification.Name, actionTitle);

			window.Title = $"{actionTitle} ({backendName})";

			window.Show();
		}

		[CanBeNull]
		public static string GetResultsPath(
			[NotNull] IQualitySpecificationReference qualitySpecification,
			[CanBeNull] string outputFolderPath)
		{
			if (outputFolderPath == null)
			{
				return null;
			}

			string specificationName =
				FileSystemUtils.ReplaceInvalidFileNameChars(
					qualitySpecification.Name, '_');

			string directoryName = $"{specificationName}_{DateTime.Now:yyyyMMdd_HHmmss}";

			string outputParentFolder = Path.Combine(outputFolderPath, "Verifications");

			string resultsPath = Path.Combine(outputParentFolder, directoryName);

			return resultsPath;
		}
	}
}

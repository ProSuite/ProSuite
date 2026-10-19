using System;
using System.IO;
using System.Windows;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.QA;

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

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

		public static string GetResultsPath(
			[NotNull] IQualitySpecificationReference qualitySpecification,
			[NotNull] string outputFolderPath)
		{
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

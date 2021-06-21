using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Threading.Tasks;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.UI.QA.VerificationProgress;

namespace ProSuite.AGP.QA.ProPlugins
{
	public abstract class VerifyVisibleExtentCmdBase : Button
	{
		protected abstract IQualityVerificationEnvironment QualityVerificationEnvironment { get; }

		protected abstract Window CreateProgressWindow(
			VerificationProgressViewModel progressViewModel);

		protected override void OnClick()
		{
			if (QualityVerificationEnvironment.CurrentQualitySpecification == null)
			{
				MessageBox.Show("No Quality Specification is selected", "Verify Extent",
				                MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			var cancellationTokenSource = new CancellationTokenSource();

			var progressTracker = new QualityVerificationProgressTracker
			                      {
				                      CancellationTokenSource = cancellationTokenSource
			                      };

			Envelope currentExtent = MapView.Active.Extent;

			string resultsPath =
				GetResultsPath(QualityVerificationEnvironment.CurrentQualitySpecification,
				               Project.Current.HomeFolderPath);

			string htmlReport = Path.Combine(resultsPath, HtmlReportName);

			var qaProgressViewmodel =
				new VerificationProgressViewModel
				{
					ProgressTracker = progressTracker,
					VerificationAction = () => Verify(currentExtent, progressTracker, resultsPath),
					ShowReportAction = verification => Process.Start(htmlReport),
					SaveAction = null,
					OpenWorkListCommand = null,
					ZoomToPerimeterCommand = null,
					FlashProgressAction = null
				};

			Window window = CreateProgressWindow(qaProgressViewmodel);

			string actionTitle = "Verify Visible Extent";

			window.Title = $"{actionTitle} ({QualityVerificationEnvironment.BackendDisplayName})";

			window.Show();
		}

		protected abstract string HtmlReportName { get; }

		private static string GetResultsPath(
			[NotNull] QualitySpecificationReference qualitySpecification,
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

		private async Task<ServiceCallStatus> Verify(
			[NotNull] Envelope currentExtent,
			[NotNull] QualityVerificationProgressTracker progressTracker,
			string resultsPath)
		{
			Task<ServiceCallStatus> verificationTask =
				await BackgroundTask.Run(
					() => QualityVerificationEnvironment.VerifyExtent(
						currentExtent, progressTracker, resultsPath),
					BackgroundProgressor.None);

			return await verificationTask;
		}
	}
}

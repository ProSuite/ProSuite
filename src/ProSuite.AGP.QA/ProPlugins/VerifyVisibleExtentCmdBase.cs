using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Threading.Tasks;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.QA.VerificationProgress;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.UI.QA.VerificationProgress;

namespace ProSuite.AGP.QA.ProPlugins
{
	public abstract class VerifyVisibleExtentCmdBase : Button
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected abstract IMapBasedSessionContext SessionContext { get; }

		protected abstract Window CreateProgressWindow(
			VerificationProgressViewModel progressViewModel);

		protected override void OnClick()
		{
			IQualityVerificationEnvironment qaEnvironment =
				Assert.NotNull(SessionContext.VerificationEnvironment);

			IQualitySpecificationReference qualitySpecification =
				qaEnvironment.CurrentQualitySpecification;

			if (qualitySpecification == null)
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

			string resultsPath = GetResultsPath(qualitySpecification,
			                                    Project.Current.HomeFolderPath);

			SpatialReference spatialRef = SessionContext.ProjectWorkspace?.ModelSpatialReference;

			var appController = new AgpBackgroundVerificationController(
				MapView.Active, currentExtent, spatialRef);

			var qaProgressViewmodel =
				new VerificationProgressViewModel
				{
					ProgressTracker = progressTracker,
					VerificationAction = () => Verify(currentExtent, progressTracker, resultsPath),
					ApplicationController = appController
				};

			Window window = CreateProgressWindow(qaProgressViewmodel);

			string actionTitle = "Verify Visible Extent";

			_msg.InfoFormat("{0}: {1}", qualitySpecification.Name, actionTitle);

			window.Title = $"{actionTitle} ({qaEnvironment.BackendDisplayName})";

			window.Show();
		}

		private static string GetResultsPath(
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

		private async Task<ServiceCallStatus> Verify(
			[NotNull] Envelope currentExtent,
			[NotNull] QualityVerificationProgressTracker progressTracker,
			string resultsPath)
		{
			Task<ServiceCallStatus> verificationTask =
				await BackgroundTask.Run(
					() =>
					{
						IQualityVerificationEnvironment qaEnvironment =
							SessionContext.VerificationEnvironment;

						Assert.NotNull(qaEnvironment);

						return qaEnvironment.VerifyExtent(
							currentExtent, progressTracker, resultsPath);
					},
					BackgroundProgressor.None);

			return await verificationTask;
		}
	}
}

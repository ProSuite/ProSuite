using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Threading.Tasks;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.QA.VerificationProgress;
using ProSuite.Commons.AGP;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.UI.QA.VerificationProgress;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace ProSuite.AGP.QA.ProPlugins
{
	public abstract class VerifyVisibleExtentCmdBase : Button
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected VerifyVisibleExtentCmdBase()
		{
			// Instead of wiring each single button and tool and calling SessionContext.CanVerifyQuality
			// for each one, the singleton event aggregator updates all at once:
			Register();
		}

		private void Register()
		{
			VerificationPlugInController.GetInstance(SessionContext).Register(this);
		}

		protected abstract IMapBasedSessionContext SessionContext { get; }

		protected abstract IProSuiteFacade ProSuiteImpl { get; }

		protected override void OnClick()
		{
			if (SessionContext?.VerificationEnvironment == null)
			{
				MessageBox.Show("No quality verification environment is configured.",
				                "Verify Extent", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			IQualityVerificationEnvironment qaEnvironment =
				Assert.NotNull(SessionContext.VerificationEnvironment);

			IQualitySpecificationReference qualitySpecification =
				qaEnvironment.CurrentQualitySpecificationReference;

			if (qualitySpecification == null)
			{
				MessageBox.Show("No Quality Specification is selected", "Verify Extent",
				                MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			var progressTracker = new QualityVerificationProgressTracker
			                      {
				                      CancellationTokenSource = new CancellationTokenSource()
			                      };

			Envelope currentExtent = MapView.Active.Extent;

			string resultsPath = VerifyUtils.GetResultsPath(qualitySpecification);

			SpatialReference spatialRef = SessionContext.ProjectWorkspace?.ModelSpatialReference;

			var appController =
				new AgpBackgroundVerificationController(ProSuiteImpl, MapView.Active, currentExtent,
				                                        spatialRef);

			var qaProgressViewmodel =
				new VerificationProgressViewModel
				{
					ProgressTracker = progressTracker,
					VerificationAction = () => Verify(currentExtent, progressTracker, resultsPath),
					ApplicationController = appController
				};

			string actionTitle = $"{qualitySpecification.Name}: Verify Visible Extent";

			Window window = VerificationProgressWindow.Create(qaProgressViewmodel);

			VerifyUtils.ShowProgressWindow(window, qualitySpecification,
			                               qaEnvironment.BackendDisplayName, actionTitle);
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

						return qaEnvironment.VerifyPerimeter(
							currentExtent, progressTracker, "visible extent", resultsPath);
					},
					BackgroundProgressor.None);

			ServiceCallStatus result = await verificationTask;

			return result;
		}
	}
}

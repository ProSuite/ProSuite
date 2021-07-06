using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Threading.Tasks;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.QA.VerificationProgress;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.UI.QA.VerificationProgress;

namespace ProSuite.AGP.QA.ProPlugins
{
	public abstract class VerifySelectionCmdBase : Button
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
				MessageBox.Show("No Quality Specification is selected", "Verify Selection",
				                MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			var progressTracker = new QualityVerificationProgressTracker
			                      {
				                      CancellationTokenSource = new CancellationTokenSource()
			                      };

			// Consider getting the extent of the selection:
			Envelope currentExtent = null; //SelectionUtils.

			string resultsPath = VerifyUtils.GetResultsPath(qualitySpecification,
			                                                Project.Current.HomeFolderPath);

			SpatialReference spatialRef = SessionContext.ProjectWorkspace?.ModelSpatialReference;

			var appController = new AgpBackgroundVerificationController(
				MapView.Active, currentExtent, spatialRef);

			var qaProgressViewmodel =
				new VerificationProgressViewModel
				{
					ProgressTracker = progressTracker,
					VerificationAction = () => Verify(progressTracker, resultsPath),
					ApplicationController = appController
				};

			string actionTitle = "Verify Selection";

			Window window = CreateProgressWindow(qaProgressViewmodel);

			VerifyUtils.ShowProgressWindow(window, qualitySpecification,
			                               qaEnvironment.BackendDisplayName, actionTitle);
		}

		private async Task<ServiceCallStatus> Verify(
			[NotNull] QualityVerificationProgressTracker progressTracker,
			string resultsPath)
		{
			var selection = await QueuedTask.Run(
				                () =>
				                {
					                return SelectionUtils.GetSelectedFeatures(MapView.Active)
					                                     .Cast<Row>().ToList();
				                });

			Task<ServiceCallStatus> verificationTask =
				await BackgroundTask.Run(
					() =>
					{
						IQualityVerificationEnvironment qaEnvironment =
							SessionContext.VerificationEnvironment;

						Assert.NotNull(qaEnvironment);

						return qaEnvironment.VerifySelection(
							selection, null, progressTracker, resultsPath);
					},
					BackgroundProgressor.None);

			ServiceCallStatus result = await verificationTask;

			if (result == ServiceCallStatus.Finished)
			{
				_msg.InfoFormat(
					"Successfully finished selection verification. The results have been saved in {0}",
					resultsPath);
			}
			else
			{
				_msg.WarnFormat("Selection verification was not finished. Status: {0}", result);
			}

			return result;
		}
	}
}

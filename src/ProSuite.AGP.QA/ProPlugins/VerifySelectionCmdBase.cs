using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.QA.VerificationProgress;
using ProSuite.AGP.WorkList;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
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
	public abstract class VerifySelectionCmdBase : ButtonCommandBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected VerifySelectionCmdBase()
		{
			Register();
		}

		private void Register()
		{
			VerificationPlugInController.GetInstance(SessionContext).Register(this);
		}

		protected abstract ISessionContext SessionContext { get; }

		protected abstract IWorkListOpener WorkListOpener { get; }

		protected virtual Func<IQualityVerificationResult, ErrorDeletionInPerimeter, bool, Task<int>>
			SaveAction => null;

		protected override async Task<bool> OnClickAsyncCore()
		{
			if (SessionContext?.VerificationEnvironment == null)
			{
				MessageBox.Show("No quality verification environment is configured.",
				                "Verify Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
				return false;
			}

			MapView mapView = MapView.Active;

			if (mapView == null)
			{
				MessageBox.Show("No active map.", "Verify Extent",
				                MessageBoxButton.OK, MessageBoxImage.Warning);
				return false;
			}

			IQualityVerificationEnvironment qaEnvironment =
				Assert.NotNull(SessionContext.VerificationEnvironment);

			IQualitySpecificationReference qualitySpecification =
				qaEnvironment.CurrentQualitySpecificationReference;

			if (qualitySpecification == null)
			{
				MessageBox.Show("No quality specification is selected", "Verify Selection",
				                MessageBoxButton.OK, MessageBoxImage.Warning);
				return false;
			}

			if (! MapUtils.HasSelection(mapView.Map))
			{
				MessageBox.Show("No selected features", "Verify Selection",
				                MessageBoxButton.OK, MessageBoxImage.Warning);
				return false;
			}

			IList<Row> selection = await QueuedTask.Run(GetRelevantSelection);

			var progressTracker = new QualityVerificationProgressTracker
			                      {
				                      CancellationTokenSource = new CancellationTokenSource()
			                      };

			// Consider getting the extent of the selection:
			Envelope currentExtent = null; //SelectionUtils.

			string resultsPath = VerifyUtils.GetResultsPath(qualitySpecification);

			SpatialReference spatialRef = SessionContext.ProjectWorkspace?.ModelSpatialReference;

			var appController = new AgpBackgroundVerificationController(WorkListOpener,
				mapView, currentExtent, spatialRef, SaveAction);

			var qaProgressViewmodel =
				new VerificationProgressViewModel
				{
					ProgressTracker = progressTracker,
					VerificationAction = () => Verify(selection, progressTracker, resultsPath),
					ApplicationController = appController,
					KeepPreviousIssuesDisabled = true
				};

			string actionTitle = $"{qualitySpecification.Name}: Verify Selection";

			Window window = VerificationProgressWindow.Create(qaProgressViewmodel);

			string backendDisplayName = Assert.NotNullOrEmpty(qaEnvironment.BackendDisplayName);

			VerifyUtils.ShowProgressWindow(window, qualitySpecification,
			                               backendDisplayName, actionTitle);

			return true;
		}

		private async Task<ServiceCallStatus> Verify(
			[NotNull] IList<Row> selection,
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

						return qaEnvironment.VerifySelection(
							selection, null, progressTracker, resultsPath);
					},
					BackgroundProgressor.None);

			ServiceCallStatus result = await verificationTask;

			return result;
		}

		private Task<IList<Row>> GetRelevantSelection()
		{
			// Check if the selected feature is part of the project workspace:
			Datastore projectWorkspaceDatastore = SessionContext.ProjectWorkspace?.Datastore;

			if (projectWorkspaceDatastore == null)
			{
				throw new InvalidOperationException("No active project workspace");
			}

			var result = new List<Row>();
			bool anySelected = false;
			foreach (Feature selectedFeature in SelectionUtils.GetSelectedFeatures(MapView.Active))
			{
				anySelected = true;
				Datastore featureDatastore = selectedFeature.GetTable().GetDatastore();

				if (WorkspaceUtils.IsSameDatastore(projectWorkspaceDatastore, featureDatastore))
				{
					result.Add(selectedFeature);
				}
			}

			if (result.Count == 0)
			{
				if (anySelected)
				{
					throw new InvalidOperationException(
						"The selection is not in the current project workspace");
				}

				throw new InvalidOperationException("No selection");
			}

			return Task.FromResult<IList<Row>>(result);
		}
	}
}

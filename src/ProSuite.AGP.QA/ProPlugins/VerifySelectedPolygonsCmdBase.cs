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
using ProSuite.Commons.AGP.Core.Spatial;
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
	public abstract class VerifySelectedPolygonsCmdBase : ButtonCommandBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected VerifySelectedPolygonsCmdBase()
		{
			Register();
		}

		private void Register()
		{
			VerificationPlugInController.GetInstance(SessionContext).Register(this);
		}

		protected abstract ISessionContext SessionContext { get; }

		protected abstract IWorkListOpener WorkListOpener { get; }

		protected virtual Action<IQualityVerificationResult, ErrorDeletionInPerimeter, bool>
			SaveAction => null;

		protected override async Task<bool> OnClickAsyncCore()
		{
			if (SessionContext?.VerificationEnvironment == null)
			{
				MessageBox.Show("No quality verification environment is configured.",
				                Caption, MessageBoxButton.OK, MessageBoxImage.Warning);
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
				MessageBox.Show("No quality specification is selected", Caption,
				                MessageBoxButton.OK, MessageBoxImage.Warning);
				return false;
			}

			if (! MapUtils.HasSelection(mapView.Map))
			{
				MessageBox.Show("No selected polygons", Caption,
				                MessageBoxButton.OK, MessageBoxImage.Warning);
				return false;
			}

			IList<Polygon> selectedPolygons = await QueuedTask.Run(GetSelectedPolygons);
			Polygon selectedPolygonGeometry = null;
			if (selectedPolygons.Count == 1)
			{
				selectedPolygonGeometry = selectedPolygons[0];
			}
			else
			{
				_msg.InfoFormat("Calculating union of {0} polygons", selectedPolygons.Count);

				await QueuedTask.Run(() => selectedPolygonGeometry =
					                           (Polygon) GeometryUtils.Union(selectedPolygons));
			}

			var progressTracker = new QualityVerificationProgressTracker
			                      {
				                      CancellationTokenSource = new CancellationTokenSource()
			                      };

			string resultsPath = VerifyUtils.GetResultsPath(qualitySpecification);

			SpatialReference spatialRef = SessionContext.ProjectWorkspace?.ModelSpatialReference;

			var appController = new AgpBackgroundVerificationController(WorkListOpener,
				mapView, selectedPolygonGeometry, spatialRef, SaveAction);

			var qaProgressViewmodel =
				new VerificationProgressViewModel
				{
					ProgressTracker = progressTracker,
					VerificationAction = () =>
						Verify(selectedPolygonGeometry, progressTracker, resultsPath),
					ApplicationController = appController
				};

			string actionTitle = $"{qualitySpecification.Name}: Verify Selection";

			Window window = VerificationProgressWindow.Create(qaProgressViewmodel);

			string backendDisplayName = Assert.NotNullOrEmpty(qaEnvironment.BackendDisplayName);

			VerifyUtils.ShowProgressWindow(window, qualitySpecification,
			                               backendDisplayName, actionTitle);

			return true;
		}

		private async Task<ServiceCallStatus> Verify(
			[NotNull] Polygon polygon,
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
							polygon, progressTracker, "selected polygons", resultsPath);
					},
					BackgroundProgressor.None);

			ServiceCallStatus result = await verificationTask;

			return result;
		}

		private Task<IList<Polygon>> GetSelectedPolygons()
		{
			// Check if the selected features are polygons:
			var result = new List<Polygon>();
			bool anySelected = false;
			foreach (Feature selectedFeature in SelectionUtils.GetSelectedFeatures(MapView.Active))
			{
				anySelected = true;

				Polygon selectedPolygon = selectedFeature.GetShape() as Polygon;

				if (selectedPolygon != null)
				{
					result.Add(selectedPolygon);
				}
			}

			if (result.Count == 0)
			{
				if (anySelected)
				{
					throw new InvalidOperationException(
						"The selection does not contain a polygon feature. Please select at least one polygon feature.");
				}

				throw new InvalidOperationException(
					"No feature is selected. Please select at least one polygon feature.");
			}

			return Task.FromResult<IList<Polygon>>(result);
		}
	}
}

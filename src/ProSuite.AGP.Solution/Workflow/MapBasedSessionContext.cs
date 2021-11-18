using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.QA;
using ProSuite.AGP.Solution.QA;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.Microservices.Client.AGP.QA;
using ProSuite.Microservices.Client.QA;

namespace ProSuite.AGP.Solution.Workflow
{
	public class MapBasedSessionContext : IMapBasedSessionContext
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private MapView _mapView;
		private IQualityVerificationEnvironment _verificationEnvironment;

		public MapBasedSessionContext()
		{
			ActiveMapViewChangedEvent.Subscribe(e => MapViewChanged(e.IncomingView));

			// TODO: Event for loading of different aprx?
			// TODO: Add layer event and if no PW is loaded: Try load
		}

		public async Task<IQualityVerificationEnvironment> InitializeVerificationEnvironment()
		{
			bool canAccessDdx = MicroServiceClient != null &&
			                    await MicroServiceClient.CanAcceptCallsAsync();

			QualityVerificationEnvironment verificationEnvironment;
			if (! canAccessDdx)
			{
				// If no client channel can be established, use XML verification provider directly
				verificationEnvironment = new QualityVerificationEnvironment();

				// TODO: Consider setting up GP verification environment and use the same Tools/Commands
			}
			else
			{
				// In case the map is already loaded: Set up the project workspace:
				await TrySelectProjectWorkspaceFromFocusMapAsync();

				verificationEnvironment =
					new QualityVerificationEnvironment(this, MicroServiceClient);

				verificationEnvironment.VerificationService =
					new VerificationServiceGrpc(MicroServiceClient)
					{
						HtmlReportName = Constants.HtmlReportName,
						VerificationReportName = Constants.VerificationReportName
					};
			}

			VerificationEnvironment = verificationEnvironment;
			VerificationEnvironment.RefreshQualitySpecifications();

			// Update the enabled / disabled reason properties of interested tools and buttons:

			return verificationEnvironment;
		}

		public IQualityVerificationEnvironment VerificationEnvironment
		{
			get => _verificationEnvironment;
			set
			{
				if (_verificationEnvironment != null)
				{
					_verificationEnvironment.QualitySpecificationsRefreshed -=
						VerificationEnvironmentQualitySpecificationsRefreshed();
				}

				_verificationEnvironment = value;

				if (_verificationEnvironment != null)
				{
					_verificationEnvironment.QualitySpecificationsRefreshed +=
						VerificationEnvironmentQualitySpecificationsRefreshed();
				}
			}
		}

		public QualityVerificationServiceClient MicroServiceClient { get; set; }

		public bool DdxAccessDisabled =>
			MicroServiceClient == null || ! MicroServiceClient.CanAcceptCalls();

		public ProjectWorkspace ProjectWorkspace { get; private set; }

		public event EventHandler ProjectWorkspaceChanged;

		public event EventHandler QualitySpecificationsRefreshed;

		public async Task<List<ProjectWorkspace>> GetProjectWorkspaceCandidates(
			ICollection<Table> objectClasses)
		{
			if (DdxAccessDisabled)
			{
				throw new InvalidOperationException(
					"No Data Dictionary based verification service.");
			}

			List<ProjectWorkspace> projectWorkspaceCandidates =
				await DdxUtils.GetProjectWorkspaceCandidates(
					objectClasses, Assert.NotNull(MicroServiceClient.DdxClient));

			return projectWorkspaceCandidates;
		}

		public async Task<bool> TrySelectProjectWorkspaceFromFocusMapAsync()
		{
			Assert.False(DdxAccessDisabled,
			             "No Data Dictionary based verification service available.");

			try
			{
				return await SelectProjectWorkspaceAsync(MapView.Active);
			}
			catch (Exception e)
			{
				_msg.Warn("Error selecting project workspace.", e);

				return false;
			}
		}

		public bool CanVerifyQuality(out string reason)
		{
			if (VerificationEnvironment == null)
			{
				reason = "No quality verification environment has been initialized.";
				return false;
			}

			if (DdxAccessDisabled)
			{
				reason = "The verification microservice is not running or unable to accept calls";
				return false;
			}

			if (ProjectWorkspace == null)
			{
				reason = "No project workspace could be inferred from the current map layers.";
				return false;
			}

			if (VerificationEnvironment.CurrentQualitySpecification == null)
			{
				reason = "No quality specification is selected.";
			}

			reason = null;
			return true;
		}

		private void MapViewChanged(MapView incomingMapView)
		{
			if (DdxAccessDisabled)
			{
				_msg.DebugFormat("MapViewChanged: No Data Dictionary access");
				return;
			}

			const string errorMessage = "Error loading quality verifications";

			_mapView = incomingMapView;

			try
			{
				Task<bool> selectProjectWorkspaceTask = SelectProjectWorkspaceAsync(_mapView);

				selectProjectWorkspaceTask.ContinueWith(
					t => { HandleExceptions(t.Exception, "Error selecting project workspace"); },
					TaskContinuationOptions.OnlyOnFaulted);
			}
			catch (Exception e)
			{
				LogException(errorMessage, e);
			}
		}

		private static void HandleExceptions(AggregateException aggregateException,
		                                     string errorMessage)
		{
			ReadOnlyCollection<Exception> inners = aggregateException?.InnerExceptions;

			if (inners != null)
			{
				foreach (Exception inner in inners)
				{
					LogException(errorMessage, inner);
				}
			}
		}

		private async Task<bool> SelectProjectWorkspaceAsync([CanBeNull] MapView mapView)
		{
			if (mapView == null)
			{
				SetProjectWorkspace(null);
				return false;
			}

			List<Table> objectClasses =
				await QueuedTask.Run(() => GetDatasets(mapView).ToList());

			List<ProjectWorkspace> projectWorkspaceCandidates =
				await DdxUtils.GetProjectWorkspaceCandidates(
					objectClasses, Assert.NotNull(MicroServiceClient.DdxClient));

			// TODO: Dialog with the choice of workspaces
			ProjectWorkspace selectedProjectWorkspace =
				projectWorkspaceCandidates.MaxElementOrDefault(p => p.Datasets.Count);

			SetProjectWorkspace(selectedProjectWorkspace);

			return selectedProjectWorkspace != null;
		}

		private static IEnumerable<Table> GetDatasets([NotNull] MapView mapView)
		{
			Map map = mapView.Map;

			if (map == null)
			{
				yield break;
			}

			//TODO: we are missing Standalone tables here, maybe other types of Layers as well
			foreach (Table table in map.GetLayers<FeatureLayer>().GetTables())
			{
				if (table.GetDatastore() is FileSystemDatastore)
				{
					// Shapefile workspaces are not supported
					continue;
				}

				yield return table;
			}
		}

		private void SetProjectWorkspace(ProjectWorkspace newProjectWorkspace)
		{
			ProjectWorkspace = newProjectWorkspace;
			ProjectWorkspaceChanged?.Invoke(this, EventArgs.Empty);
		}

		private static void LogException(string errorMessage, Exception exception)
		{
			_msg.Error($"{errorMessage}: {exception.Message}", exception);
		}

		private EventHandler VerificationEnvironmentQualitySpecificationsRefreshed()
		{
			return (sender, e) =>
				QualitySpecificationsRefreshed?.Invoke(this, EventArgs.Empty);
		}
	}
}

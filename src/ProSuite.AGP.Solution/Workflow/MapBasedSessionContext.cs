using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
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

		private EventHandler VerificationEnvironmentQualitySpecificationsRefreshed()
		{
			return (sender, e) =>
				QualitySpecificationsRefreshed?.Invoke(this, EventArgs.Empty);
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
				throw new InvalidOperationException("No Data Dictionary access");
			}

			List<ProjectWorkspace> projectWorkspaceCandidates =
				await DdxUtils.GetProjectWorkspaceCandidates(
					objectClasses, Assert.NotNull(MicroServiceClient.DdxClient));

			return projectWorkspaceCandidates;
		}

		public async Task<bool> TrySelectProjectWorkspaceFromFocusMapAsync()
		{
			if (DdxAccessDisabled)
			{
				throw new InvalidOperationException("No Data Dictionary access");
			}

			return await SelectProjectWorkspaceAsync(MapView.Active);
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

			foreach (Table table in map.GetTables())
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
	}
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.Microservices.Client.AGP.QA;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.AGP.Solution.QA
{
	public class QualityVerificationEnvironment : IQualityVerificationEnvironment
	{
		// TODO: Create WorkContext (work unit) based on map content (with or without DDX access)
		//private readonly IWorkContext _workContext;

		private const string _contextTypeWorkUnit = "Work Unit";
		private const string _contextTypePerimeter = "Perimeter";

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private MapView _mapView;
		private readonly QualityVerificationServiceClient _client;
		private readonly string _verificationOutputDirectory;

		private IList<QualitySpecificationReference> _qualitySpecifications;

		public QualityVerificationEnvironment(
			//IWorkContext workContext,
			MapView mapView,
			QualityVerificationServiceClient client,
			string verificationOutputDirectory)
		{
			//_workContext = workContext;

			_mapView = mapView;
			_client = client;
			_verificationOutputDirectory = verificationOutputDirectory;
		}

		// TODO: Handle layer added event, if current project workspace is null, load entities
		//public void MapLayersChanged(IEnumerable<Layer> layers)
		//{
		//	_editableDatasets.Clear();

		//	foreach (Layer layer in layers)
		//	{
		//		if (layer is FeatureLayer featureLayer)
		//		{
		//			_editableDatasets.Add(featureLayer.GetTable());
		//		}
		//	}

		//	// TODO: Tables?

		//	LoadQualitySpecifications();
		//}

		// TODO: Also handle when a new project is loaded
		public void MapViewChanged(MapView mapView)
		{
			const string errorMessage = "Error loading quality verifications";

			_mapView = mapView;

			try
			{
				if (_mapView == null)
				{
					_qualitySpecifications?.Clear();
					SelectCurrentQualitySpecification(_qualitySpecifications);
					return;
				}

				Task<bool> task = LoadEntitiesAsync();

				task.ContinueWith(t =>
				                  {
					                  ReadOnlyCollection<Exception> inners =
						                  t.Exception?.InnerExceptions;

					                  if (inners != null)
					                  {
						                  foreach (Exception inner in inners)
						                  {
							                  LogException(errorMessage, inner);
						                  }
					                  }

					                  //var aggException = t.Exception?.Flatten();

					                  //LogException(errorMessage, aggException);
				                  },
				                  TaskContinuationOptions.OnlyOnFaulted);
			}
			catch (Exception e)
			{
				LogException(errorMessage, e);
			}
		}

		private static void LogException(string errorMessage, Exception exception)
		{
			_msg.Error($"{errorMessage}: {exception.Message}", exception);
		}

		private bool IncludeHiddenSpecifications { get; set; }

		private ProjectWorkspace ProjectWorkspace { get; set; }

		/// <summary>
		/// The last current quality specification. This can be used to restore the state of the UI.
		/// </summary>
		public int LastCurrentSpecificationId { get; set; } = -1;

		public QualitySpecificationReference CurrentQualitySpecification { get; set; }

		public IList<QualitySpecificationReference> QualitySpecifications =>
			_qualitySpecifications ?? new List<QualitySpecificationReference>(0);

		public void RefreshQualitySpecifications()
		{
			// TODO
		}

		public event EventHandler QualitySpecificationsRefreshed;

		public string BackendDisplayName => _client.HostName;

		public async Task<ServiceCallStatus> VerifyExtent(
			Envelope extent,
			QualityVerificationProgressTracker progress,
			[CanBeNull] string resultsPath)
		{
			QualitySpecificationReference specification =
				Assert.NotNull(CurrentQualitySpecification);

			VerificationRequest request =
				await QueuedTask.Run(
					() => QAUtils.CreateRequest(ProjectWorkspace, _contextTypePerimeter, "map name",
					                            specification, extent));

			if (! string.IsNullOrEmpty(resultsPath))
			{
				string htmlReport = Path.Combine(resultsPath, Constants.HtmlReportName);
				string xmlReport = Path.Combine(resultsPath, Constants.VerificationReportName);
				string gdbDir = Path.Combine(resultsPath, "issues.gdb");

				request.Parameters.HtmlReportPath = htmlReport;
				request.Parameters.VerificationReportPath = xmlReport;
				request.Parameters.IssueFileGdbPath = gdbDir;
			}

			QAUtils.SetVerificationParameters(
				request, GetTileSize(ProjectWorkspace), false, true,
				false);

			return await QAUtils.Verify(Assert.NotNull(_client.QaClient), request, progress);
		}

		private double GetTileSize(ProjectWorkspace projectWorkspace)
		{
			// TODO
			return -1;
		}

		public int ProjectId { get; set; } = -1;

		private async Task<bool> LoadEntitiesAsync()
		{
			if (_mapView == null)
			{
				return false;
			}

			List<Table> objectClasses =
				await QueuedTask.Run(() => GetDatasets().ToList());

			List<ProjectWorkspace> projectWorkspaceCandidates =
				await DdxUtils.GetProjectWorkspaceCandidates(
					objectClasses, Assert.NotNull(_client.DdxClient));

			ProjectWorkspace =
				projectWorkspaceCandidates.MaxElementOrDefault(p => p.Datasets.Count);

			if (ProjectWorkspace == null)
			{
				return false;
			}

			ProjectId = ProjectWorkspace.ProjectId;
			//Datasets = projectWorkspace.Datasets.ToList();

			IList<QualitySpecificationReference> result =
				await DdxUtils.LoadSpecificationsRpcAsync(ProjectWorkspace.Datasets,
				                                          IncludeHiddenSpecifications,
				                                          Assert.NotNull(_client.DdxClient));

			// if there's a current quality specification, check if it is valid
			if (CurrentQualitySpecification != null &&
			    ! result.Contains(CurrentQualitySpecification))
			{
				CurrentQualitySpecification = null;
			}

			// if there is no valid current specification, select one 
			if (CurrentQualitySpecification == null)
			{
				SelectCurrentQualitySpecification(result);
			}

			_qualitySpecifications = result;

			QualitySpecificationsRefreshed?.Invoke(this, EventArgs.Empty);

			return _qualitySpecifications.Count > 0;
		}

		private IEnumerable<Table> GetDatasets()
		{
			IReadOnlyList<Layer> layers = _mapView.Map.GetLayersAsFlattenedList();

			foreach (Layer layer in layers)
			{
				if (layer is FeatureLayer fl)
				{
					Table table = fl.GetTable();

					if (table.GetDatastore() is FileSystemDatastore)
					{
						// Shapefile workspaces are not supported
						continue;
					}

					yield return table;
				}
			}
		}

		private void SelectCurrentQualitySpecification(
			[CanBeNull] IList<QualitySpecificationReference> qualitySpecifications)
		{
			if (qualitySpecifications == null || qualitySpecifications.Count == 0)
			{
				CurrentQualitySpecification = null;
				return;
			}

			QualitySpecificationReference result;

			if (LastCurrentSpecificationId >= 0)
			{
				// try to load the last one used
				result = qualitySpecifications.FirstOrDefault(
					qualitySpecification => qualitySpecification.Id == LastCurrentSpecificationId);

				if (result != null)
				{
					CurrentQualitySpecification = result;
					return;
				}
			}

			CurrentQualitySpecification =
				qualitySpecifications.Count == 0 ? null : qualitySpecifications[0];

			StoreLastQualitySpecificationId();
		}

		private void StoreLastQualitySpecificationId()
		{
			if (CurrentQualitySpecification == null)
			{
				return;
			}

			LastCurrentSpecificationId = CurrentQualitySpecification.Id;
		}

		private static DateTime GetTimeout()
		{
			DateTime timeout = DateTime.Now.AddSeconds(10).ToUniversalTime();

			return timeout;
		}
	}

	//public interface IWorkContext
	//{
	//	ProductionModel Model { get; }

	//	string Name { get; }

	//	/// <summary>
	//	/// Gets the editable datasets of a given type.
	//	/// </summary>
	//	/// <typeparam name="T">The dataset type to return.</typeparam>
	//	/// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the
	//	/// conditions of the datasets to search for.</param>
	//	/// <param name="includeDeleted">if set to <c>true</c> deleted datasets are included 
	//	/// in the result, otherwise they are excluded.</param>
	//	/// <returns></returns>
	//	[NotNull]
	//	IList<T> GetEditableDatasets<T>([CanBeNull] Predicate<T> match = null,
	//	                                bool includeDeleted = false) where T : class, IDdxDataset;
	//}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.Microservices.Client.AGP;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.AGP.Solution.QA
{
	public class QualityVerificationEnvironment : IQualityVerificationEnvironment
	{
		// TODO: Consider separate project AGP.QA
		// TODO: Create WorkContext (work unit) based on map content (with or without DDX access)
		//private readonly IWorkContext _workContext;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private IList<Table> _editableDatasets;
		private MapView _mapView;
		private readonly QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient _ddxClient;

		private IList<QualitySpecificationRef> _qualitySpecifications;

		public QualityVerificationEnvironment(
			//IWorkContext workContext,
			IList<Table> editableDatasets,
			QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient ddxClient)
		{
			//_workContext = workContext;
			_editableDatasets = editableDatasets;
			_ddxClient = ddxClient;
		}

		public QualityVerificationEnvironment(
			//IWorkContext workContext,
			MapView mapView,
			QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient ddxClient)
		{
			//_workContext = workContext;
			_editableDatasets = null;
			_mapView = mapView;
			_ddxClient = ddxClient;
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

			try
			{
				_mapView = mapView;

				Task<bool> task = LoadEntitiesAsync();

				task.ContinueWith(t =>
				                  {
					                  foreach (Exception inner in t.Exception.InnerExceptions)
					                  {
						                  LogException(errorMessage, inner);
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

		/// <summary>
		/// The last current quality specification. This can be used to restore the state of the UI.
		/// </summary>
		public int LastCurrentSpecificationId { get; set; } = -1;

		public QualitySpecificationRef CurrentQualitySpecification { get; set; }

		public IList<QualitySpecificationRef> QualitySpecifications =>
			_qualitySpecifications ?? new List<QualitySpecificationRef>(0);

		public void RefreshQualitySpecifications()
		{
			// TODO
		}

		public event EventHandler QualitySpecificationsRefreshed;

		internal List<DatasetMsg> Datasets { get; set; }

		internal int ProjectId { get; set; } = -1;

		private async Task<bool> LoadEntitiesAsync()
		{
			if (_mapView == null)
			{
				return false;
			}

			GetProjectWorkspacesRequest request =
				await QueuedTask.Run(CreateProjectWorkspacesRequest);

			if (request.ObjectClasses.Count > 0)
			{
				var projectWorkspaceMsg = await LoadProjectWorkspaceAsync(request);

				IList<QualitySpecificationRef> result =
					await LoadSpecificationsRpcAsync(projectWorkspaceMsg.DatasetIds);

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
			}

			return _qualitySpecifications.Count > 0;
		}

		private GetProjectWorkspacesRequest CreateProjectWorkspacesRequest()
		{
			var request = new GetProjectWorkspacesRequest();

			List<WorkspaceMsg> workspaceMessages = new List<WorkspaceMsg>();

			foreach (Table table in GetDatasets())
			{
				ObjectClassMsg objectClassMsg = ProtobufConversionUtils.ToObjectClassMsg(table);

				request.ObjectClasses.Add(objectClassMsg);

				WorkspaceMsg workspaceMsg =
					workspaceMessages.FirstOrDefault(
						wm => wm.WorkspaceHandle == objectClassMsg.WorkspaceHandle);

				if (workspaceMsg == null)
				{
					using (Datastore datastore = table.GetDatastore())
					{
						workspaceMsg = ProtobufConversionUtils.ToWorkspaceRefMsg(datastore);

						workspaceMessages.Add(workspaceMsg);
					}
				}
			}

			request.Workspaces.AddRange(workspaceMessages);

			return request;
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

		private async Task<IList<QualitySpecificationRef>> LoadSpecificationsRpcAsync(
			[NotNull] IList<int> datasetIds)
		{
			GetSpecificationsRequest request = new GetSpecificationsRequest()
			                                   {
				                                   IncludeHidden = IncludeHiddenSpecifications
			                                   };

			request.DatasetIds.AddRange(datasetIds);

			DateTime timeout = GetTimeout();

			GetSpecificationsResponse response =
				await _ddxClient.GetQualitySpecificationsAsync(request, null, timeout);

			var result = new List<QualitySpecificationRef>();

			foreach (QualitySpecificationRefMsg specificationMsg in response.QualitySpecifications)
			{
				var specification =
					new QualitySpecificationRef(specificationMsg.QualitySpecificationId,
					                            specificationMsg.Name);

				result.Add(specification);
			}

			return result;
		}

		private async Task<ProjectWorkspaceMsg> LoadProjectWorkspaceAsync(
			[NotNull] GetProjectWorkspacesRequest request)
		{
			DateTime timeout = GetTimeout();

			GetProjectWorkspacesResponse response =
				await _ddxClient.GetProjectWorkspacesAsync(request, null, timeout);

			// TODO: Use the dialog to select one, in case there are several
			ProjectWorkspaceMsg result =
				response.ProjectWorkspaces.MaxElementOrDefault(p => p.DatasetIds.Count);

			if (result != null)
			{
				ProjectId = result.ProjectId;

				Datasets = response.Datasets.ToList();
			}

			return result;
		}

		private void SelectCurrentQualitySpecification(
			[NotNull] IList<QualitySpecificationRef> qualitySpecifications)
		{
			Assert.ArgumentNotNull(qualitySpecifications, nameof(qualitySpecifications));

			if (qualitySpecifications.Count == 0)
			{
				return;
			}

			QualitySpecificationRef result;

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

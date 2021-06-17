using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using ProSuite.Microservices.Client.AGP.QA;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.AGP.QA
{
	public class QualityVerificationEnvironment : IQualityVerificationEnvironment
	{
		// TODO: Create WorkContext (work unit) based on map content (with or without DDX access)
		//private readonly IWorkContext _workContext;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private MapView _mapView;
		private readonly QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient _ddxClient;

		private IList<QualitySpecificationReference> _qualitySpecifications;

		public QualityVerificationEnvironment(
			//IWorkContext workContext,
			MapView mapView,
			QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient ddxClient)
		{
			//_workContext = workContext;

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

			_mapView = mapView;

			try
			{
				if (_mapView == null)
				{
					_qualitySpecifications.Clear();
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
				await DdxUtils.GetProjectWorkspaceCandidates(objectClasses, _ddxClient);

			ProjectWorkspace projectWorkspace =
				projectWorkspaceCandidates.MaxElementOrDefault(p => p.Datasets.Count);

			if (projectWorkspace == null)
			{
				return false;
			}

			ProjectId = projectWorkspace.ProjectId;
			//Datasets = projectWorkspace.Datasets.ToList();

			IList<QualitySpecificationReference> result =
				await DdxUtils.LoadSpecificationsRpcAsync(projectWorkspace.Datasets,
				                                          IncludeHiddenSpecifications, _ddxClient);

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
			[NotNull] IList<QualitySpecificationReference> qualitySpecifications)
		{
			Assert.ArgumentNotNull(qualitySpecifications, nameof(qualitySpecifications));

			if (qualitySpecifications.Count == 0)
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

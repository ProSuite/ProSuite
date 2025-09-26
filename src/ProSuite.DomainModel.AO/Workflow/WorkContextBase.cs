using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Notifications;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.Workflow
{
	public abstract class WorkContextBase : VersionedEntityWithMetadata, IWorkContext
	{
		public bool IsPrimaryWorkspaceBeingEdited()
		{
			return ((IWorkspaceEdit) PrimaryWorkspaceContext.Workspace).IsBeingEdited();
		}

		public abstract bool CanWriteIssues { get; }

		public virtual bool CanNavigateIssues => CanWriteIssues;

		public abstract void UpdateCanWriteIssues();

		public IEnumerable<INotification> CannotWriteIssuesReasons
			=> CannotWriteIssuesReasonsCore;

		public IEnumerable<INotification> CannotNavigateIssuesReasons
			=> CannotNavigateIssuesReasonsCore;

		public abstract IWorkspaceContext PrimaryWorkspaceContext { get; }

		public IDdxDataset GetDataset(IDatasetName datasetName, bool isValid)
		{
			return ModelContextUtils.GetDataset(datasetName, isValid,
			                                    PrimaryWorkspaceContext, Model);
		}

		ICollection<Dataset> IVerificationContext.GetVerifiedDatasets()
		{
			IList<Dataset> result = GetEditableDatasets<Dataset>();

			AddDependentDatasets(result);

			return result;
		}

		/// <summary>
		/// Allows derived classes to add dependent datasets to the list of verified datasets that
		/// are not directly editable in the work unit but should be included in the verification.
		/// </summary>
		/// <param name="toVerifiedDatasets"></param>
		protected virtual void AddDependentDatasets([NotNull] IList<Dataset> toVerifiedDatasets) { }

		public abstract SpatialReferenceDescriptor SpatialReferenceDescriptor { get; }

		public abstract ErrorLineDataset LineIssueDataset { get; }

		public abstract ErrorPolygonDataset PolygonIssueDataset { get; }

		public abstract ErrorMultipointDataset MultipointIssueDataset { get; }

		public abstract ErrorMultiPatchDataset MultiPatchIssueDataset { get; }

		public abstract ErrorTableDataset NoGeometryIssueDataset { get; }

		public abstract bool IsEditableInCurrentState(
			[NotNull] IDdxDataset dataset,
			[CanBeNull] NotificationCollection notifications);

		/// <summary>
		/// Determines whether the specified object category is generally editable in this work unit.
		/// This check is based only on the list of editable datasets, plus the general 
		/// editability of special dataset types. 
		/// </summary>
		/// <param name="objectCategory">The object category.</param>
		/// <returns>
		/// 	<c>true</c> if the specified category is generally editable in this work unit; 
		///     otherwise, <c>false</c>.
		/// </returns>
		public bool IsEditable([NotNull] ObjectCategory objectCategory)
		{
			Assert.ArgumentNotNull(objectCategory, nameof(objectCategory));

			return ! objectCategory.Deleted && IsEditable(objectCategory.ObjectDataset);
		}

		/// <summary>
		/// Determines whether the specified object category is editable in the work unit, in 
		/// the current state of the work unit (but independent of the privileges of the user).
		/// </summary>
		/// <param name="objectCategory">The object category.</param>
		/// <returns>
		/// 	<c>true</c> if the specified object category is generally editable in the
		///     current state of the work unit; otherwise, <c>false</c>.
		/// </returns>
		public bool IsEditableInCurrentState([NotNull] ObjectCategory objectCategory)
		{
			Assert.ArgumentNotNull(objectCategory, nameof(objectCategory));

			return ! objectCategory.Deleted &&
			       IsEditableInCurrentState(objectCategory.ObjectDataset);
		}

		#region IWorkContext Members

		public abstract IFeatureWorkspace OpenFeatureWorkspace();

		public IWorkspace OpenWorkspace()
		{
			return (IWorkspace) OpenFeatureWorkspace();
		}

		public abstract ProductionModel Model { get; }

		public abstract string Name { get; }

		public abstract bool CanOpen(IDdxDataset dataset);

		/// <summary>
		/// Determines whether the specified dataset is editable in the work unit, in 
		/// the current state of the work unit (but independent of the privileges of the user).
		/// </summary>
		/// <param name="dataset">The dataset.</param>
		/// <returns>
		/// 	<c>true</c> if the specified dataset is generally editable in the
		///     current state of the work unit; otherwise, <c>false</c>.
		/// </returns>
		public bool IsEditableInCurrentState(IDdxDataset dataset)
		{
			return IsEditableInCurrentState(dataset, null);
		}

		public abstract IList<T> GetEditableDatasets<T>(Predicate<T> match = null,
		                                                bool includeDeleted = false)
			where T : class, IDdxDataset;

		public abstract bool IsEditable(IDdxDataset dataset);

		/// <summary>
		/// Returns the Dataset for a given object class, if that object class is part 
		/// of the list of editable dataset. 
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <returns>Dataset, or null if the given object class is not part of the editable 
		/// datasets.</returns>
		public IObjectDataset GetEditableDataset(IObjectClass objectClass)
		{
			Dataset dataset = PrimaryWorkspaceContext.GetDatasetByGdbName(
				DatasetUtils.GetName(objectClass));

			var objectDataset = dataset as IObjectDataset;
			if (objectDataset == null)
			{
				return null;
			}

			if (! IsEditable(objectDataset))
			{
				return null;
			}

			return WorkspaceUtils.IsSameDatabase(PrimaryWorkspaceContext.Workspace,
			                                     DatasetUtils.GetWorkspace(objectClass))
				       ? objectDataset
				       : null;
		}

		public abstract IWorkspaceContext GetWorkspaceContext(IDdxDataset dataset);

		public IFeatureClass OpenFeatureClass(IVectorDataset dataset)
		{
			return (IFeatureClass) OpenObjectClass(dataset);
		}

		public ITable OpenTable(IObjectDataset dataset)
		{
			return (ITable) OpenObjectClass(dataset);
		}

		public abstract IObjectClass OpenObjectClass(IObjectDataset dataset);

		public abstract TopologyReference OpenTopology(ITopologyDataset dataset);

		public abstract RasterDatasetReference OpenRasterDataset(IDdxRasterDataset dataset);

		public abstract TerrainReference OpenTerrainReference(ISimpleTerrainDataset dataset);

		public abstract MosaicRasterReference OpenSimpleRasterMosaic(IRasterMosaicDataset dataset);

		public abstract IRelationshipClass OpenRelationshipClass(Association association);

		#endregion

		[NotNull]
		protected NotificationCollection CannotWriteIssuesReasonsCore { get; } =
			new NotificationCollection();

		[NotNull]
		protected NotificationCollection CannotNavigateIssuesReasonsCore { get; } =
			new NotificationCollection();
	}
}

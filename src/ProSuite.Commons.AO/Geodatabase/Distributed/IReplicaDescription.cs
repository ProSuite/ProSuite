using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.Distributed
{
	public interface IReplicaDescription
	{
		[CanBeNull]
		IGeometry FilterGeometry { get; set; }

		/// <summary>
		/// The replica's dataset names, i.e. the feature datasets and standalone datasets
		/// to be included in the replica.
		/// </summary>
		ICollection<IDataset> Datasets { get; }

		IFeatureWorkspace ParentWorkspace { get; }

		/// <summary>
		/// Returns the relevant relationship class infos which can be configured by client code.
		/// </summary>
		/// <returns></returns>
		IList<IRelationshipClassInfo> GetRelationshipClassInfos();

		/// <summary>
		/// Returns all the (previously configured) relationship class infos for the specified table.
		/// </summary>
		/// <param name="relatedClass"></param>
		/// <param name="relationToParentOnly">Determines whether the relationship class should
		/// be returned only if it relates the specified table to a (spatial) dataset that
		/// controls the related records in the specified table</param>
		/// <returns></returns>
		IEnumerable<IRelationshipClassInfo> GetRelationshipClassInfos(
			[NotNull] IObjectClass relatedClass,
			bool relationToParentOnly);

		/// <summary>
		/// Returns the relationship class chains for the specified table. A relationship class
		/// chain is a list of relationship classes that connect the specified table to a
		/// feature class that is part of the replica's root datasets.
		/// </summary>
		/// <param name="relatedClass"></param>
		/// <param name="relationToParentOnly"></param>
		/// <returns></returns>
		IEnumerable<List<IRelationshipClass>> GetRelationshipClassChains(
			[NotNull] IObjectClass relatedClass,
			bool relationToParentOnly);

		/// <summary>
		/// Returns the dataset filter to be configured for the specified dataset.
		/// </summary>
		/// <param name="dataset"></param>
		/// <returns></returns>
		[CanBeNull]
		IDatasetReplicaFilter GetDatasetFilter(IDataset dataset);

		/// <summary>
		/// Excludes the specified dataset from the replica.
		/// </summary>
		/// <param name="dataset">The dataset to evaluate for exclusion. Cannot be null.</param>
		/// <returns><see langword="true"/> if the dataset was successfully removed from the
		/// list of datasets; otherwise, <see langword="false"/>.</returns>
		bool ExcludeTable(IDataset dataset);
	}
}
